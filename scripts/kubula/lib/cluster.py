import re
import time
import os
import json
import subprocess
import pyone


def create_cluster(client: pyone.OneServer, home: str, credentials: dict, config: dict):
    master_template = ""
    slave_template = ""

    with open('./Master.def', 'r') as file:
        data = file.read()
        for key, value in config.items():
            data = re.sub("<\\$" + key + ">", str(value), data)
        for key, value in credentials.items():
            data = re.sub("<\\$" + key + ">", str(value), data)
        master_template = data

    with open('./Slave.def', 'r') as file:
        data = file.read()
        for key, value in config.items():
            data = re.sub("<\\$" + key + ">", str(value), data)
        for key, value in credentials.items():
            data = re.sub("<\\$" + key + ">", str(value), data)
        slave_template = data


    #print(master_template)
    #print(slave_template)
    
    print("Allocating kubernetes master VM")
    named_master_template = re.sub("<\\$name>", "kubula-0", master_template)
    master_id = client.vm.allocate(named_master_template)
    
    print("Waiting for master to be ready before creating slaves. This may take several minutes.")
    master = None
    master_state = 0
    while(master_state < 3): #3 == RUNNING
        print("Polling master state..")
        master = client.vm.info(master_id)
        #print(master, vars(master))
        master_state = master.LCM_STATE
        time.sleep(10)
    
    master_ip = master.TEMPLATE['CONTEXT']['ETH0_IP']
    print("Master running @ "+master_ip)
    
    #allocate slave VMS
    print("Allocating slaves")
    slave_ids = []
    for i in range(config['num_slaves']):
        named_slave_template = re.sub("<\\$name>", "kubula-"+str(i+1), slave_template)
        slave_ids.append(client.vm.allocate(named_slave_template))
    
    time.sleep(45)#wait an extra 45 seconds for kube config init on master..
    print("Copying kube config from master")
    kubeconfig_loc = os.path.join(home, ".kube", "config")
    p = subprocess.call(['scp', '-o', 'StrictHostKeyChecking=no', 'ubuntu@' + master_ip + ':/home/ubuntu/.kube/config', kubeconfig_loc])
    if(p != 0):
        raise SystemError("Kube config could not be copied from master node, exit code: " + str(p))

    try:
        #dump deployment info on disk
        deployment_info = {"master_id": master_id, "slave_ids": slave_ids}
        dump_deployment_info(home, deployment_info, config['cluster_name'])
        print("Deployment info sucessfully dumped on disk.")
    except:
        print("Failed to dump deployment info on disk. Deployment must be manually deleted from OpenNebula")

def wait_for_kubernetes_slaves(config: dict):
    print("Waiting for slaves to register with the kubernetes cluster")
    nodenames = []
    while len(nodenames) != config["num_slaves"]:
        time.sleep(10)
        k = subprocess.Popen(("kubectl", "get", "nodes", ), stdout=subprocess.PIPE)
        k.wait()
        try:
            f = subprocess.Popen(("findstr", "slave"), stdin=k.stdout, stdout=subprocess.PIPE)
            output = subprocess.check_output(("findstr", "Ready"), stdin=f.stdout)
        except subprocess.CalledProcessError as ex:
            if(ex.returncode == 1):
                continue #exit-code 1 means no search results.. just keep trying
            else:
                raise
        output = str(output)
        output = output.replace('b\'', '').replace('\'','').replace('\\n',',')
        nodenames = list(filter(None, output.split(",")))
    print("Test deployment by running 'kubectl get nodes'")    


def dump_deployment_info(homePath:str, deployment_info: dict, clusterName: str):
    print("Deployment info", deployment_info)
    kubulaPath = str(os.path.join(homePath, ".kubula"))
    if not os.path.exists(kubulaPath):
        os.makedirs(kubulaPath)
    with open(str(os.path.join(kubulaPath ,"." + clusterName)), 'w+') as deployment_file:
        json.dump(deployment_info, deployment_file)

def get_deployment_info(homePath:str, clusterName: str):
    kubulaPath = str(os.path.join(homePath, ".kubula"))
    if not os.path.exists(kubulaPath):
        os.makedirs(kubulaPath)
    with open(str(os.path.join(kubulaPath ,"." + clusterName)), 'r') as deployment_file:
        return json.loads(deployment_file.read())


def find_missing_ips(homePath, clusterName, client):
    expectedIps = find_expected_ips(homePath, clusterName, client)
    knownIps = find_known_ips()
    return set(expectedIps) - set(knownIps)


def find_known_ips():
    outputstr = str(subprocess.check_output(['kubectl', 'get','nodes', '--selector=!node-role.kubernetes.io/master','-o=custom-columns=ExternalIp:status.addresses[0].address',]))
    for substr in outputstr.replace('b\"', '').replace('\"','').split("\\n")[1::]:
        if(len(substr) >= 7):
            yield substr.strip()

def find_expected_ips(homePath, clusterName, client):
    deployment = get_deployment_info(homePath, clusterName)
    for slave_id in deployment['slave_ids']:
        slave = client.vm.info(slave_id)
        yield slave.TEMPLATE['CONTEXT']['ETH0_IP'].strip()

def test(credentials, config):
    print("empty")