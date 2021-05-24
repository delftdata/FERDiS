import json
import os
import subprocess
import pyone
import sys
#import warnings
#warnings.filterwarnings("ignore")

from lib.cluster import create_cluster, wait_for_kubernetes_slaves, get_deployment_info, find_missing_ips

#NOTE: used in cluster naming, cluster naming is used in some DNS stage, therefore: ensure this number is unique per experiment!
experiment_num = 429

def main():
    args = sys.argv[1:]
    # create_cluster and join_cluster scripts in instakubebase image may be relevant (@Pedro)
    #
    cred = json.load(open('credentials.json')) # username, password, endpoint, ssh_key
    conf = json.load(open('cluster-config.json')) #master/slave_cpu, _memory, _disk + num_slaves + cluster_name

    conf['cluster_name'] = conf['cluster_name'] + str(experiment_num)
    
    client = pyone.OneServer(cred['endpoint'], session=(cred['username'] + ':' + cred['password']))
    home = os.environ['USERPROFILE']
    
    if(args[0] == "create"):
        create_cluster(client, home, cred, conf)
        wait_for_kubernetes_slaves(conf)

    if(args[0] == "patch"):
        for ip in find_missing_ips(home, conf['cluster_name'], client):
            print("Attempting to reconnect: " + ip)
            trigger_rejoin(ip, conf['cluster_name'])
        wait_for_kubernetes_slaves(conf)

    if(args[0] == "regcred"):
        add_docker_pull_service_account()

    if(args[0] == "delete"):
        deployment_info = get_deployment_info(home, conf['cluster_name'])
        client.vm.action('terminate', deployment_info['master_id'])
        for slave_id in deployment_info['slave_ids']:
            client.vm.action('terminate', slave_id)

    print("Success")


def trigger_rejoin(ip, cluster_name):
    subprocess.call(['ssh', 'ubuntu@'+ip, './join_cluster.sh '+cluster_name, '-o StrictHostKeyChecking=no'])


def add_docker_pull_service_account():
    print("adding pubregcred serviceaccount to kubernetes") #allows utilising the unlimited docker pulls from a docker hub subscription
    subprocess.call(['kubectl', 'apply', '-f', "./pubregcred.yaml"])
    subprocess.Popen('kubectl patch serviceaccount default -p \'{"imagePullSecrets": [{"name": "pubregcred"}]}\'',
                     shell=True, stdout=open(os.devnull, 'wb'), stderr=subprocess.STDOUT, close_fds=True)

if __name__ == "__main__":
    main()

