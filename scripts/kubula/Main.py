import json
import os
import subprocess
#import warnings
#warnings.filterwarnings("ignore")

from lib.cluster import create_cluster, test

#NOTE: used in cluster naming, cluster naming is used in some DNS stage, therefore: ensure this number is unique per experiment!
experiment_num = 4

def main():
    #TODO: CLUSTER NAME BASED ON MAGIC NUMBER PEDRO MENTIONED
    # create_cluster and join_cluster scripts
    #
    cred = json.load(open('credentials.json')) # username, password, endpoint, ssh_key
    conf = json.load(open('cluster-config.json')) #master/slave_cpu, _memory, _disk + num_slaves + cluster_name


    conf['cluster_name'] = conf['cluster_name'] + str(experiment_num)
    create_cluster(cred, conf)

    print("adding pubregcred serviceaccount to kubernetes") #allows utilising the unlimited docker pulls from a docker hub subscription
    subprocess.call(['kubectl', 'apply', '-f', "./pubregcred.yaml"])
    subprocess.Popen('kubectl patch serviceaccount default -p \'{"imagePullSecrets": [{"name": "pubregcred"}]}\'',
                     shell=True, stdout=open(os.devnull, 'wb'), stderr=subprocess.STDOUT, close_fds=True)

    print("Success")

if __name__ == "__main__":
    main()

