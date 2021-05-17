import re
import time
import os
import json
import subprocess
import pyone


def create_cluster(credentials: dict, config: dict):
    client = pyone.OneServer(credentials['endpoint'], session=(credentials['username'] + ':' + credentials['password']))
    home = os.environ['USERPROFILE']

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
    time.sleep(30)#wait an extra 30 seconds for kube config init..
    print("Copying kube config from master")
    kubeconfig_loc = os.path.join(home, ".kube", "config")
    p = subprocess.call(['scp', '-o', 'StrictHostKeyChecking=no', 'ubuntu@' + master_ip + ':/home/ubuntu/.kube/config', kubeconfig_loc])
    if(p != 0):
        raise SystemError("Kube config could not be copied from master node, exit code: " + str(p))

    #allocate slave VMS
    print("Allocating slaves")
    slave_ids = []
    for i in range(config['num_slaves']):
        named_slave_template = re.sub("<\\$name>", "kubula-"+str(i+1), slave_template)
        slave_ids.append(client.vm.allocate(named_slave_template))

    #dump deployment info on disk
    deployment_info = {"master_id": master_id, "slave_ids": slave_ids}
    print("Deployment info", deployment_info)
    with open(str(os.path.join(home, "." + config['cluster_name'])), 'w+') as deployment_file:
        json.dump(deployment_info, deployment_file)

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
    print("Test your deployment by running 'kubectl get nodes'")




def test(credentials, config):
    print("empty")