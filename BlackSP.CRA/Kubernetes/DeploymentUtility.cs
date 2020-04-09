using BlackSP.CRA.Configuration;
using BlackSP.CRA.Configuration.Operators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlackSP.CRA.Kubernetes
{
    public class KubernetesDeploymentUtility
    {
        private ICollection<IOperatorConfigurator> _configurators;
        private string lastWrittenYamlFile;
        private string K8sNamespace => "blacksp";

        public KubernetesDeploymentUtility()
        {}
        
        public KubernetesDeploymentUtility(ICollection<IOperatorConfigurator> configurators)
        {
            _configurators = configurators ?? throw new ArgumentNullException(nameof(configurators));
            lastWrittenYamlFile = string.Empty;
        }

        public KubernetesDeploymentUtility With(ICollection<IOperatorConfigurator> configurators)
        {
            _configurators = configurators;
            return this;
        }

        public void WriteDeploymentYaml()
        {
            lastWrittenYamlFile = GetCurrentProjectPath("deployment.yaml");
            File.WriteAllText(lastWrittenYamlFile, GetDeploymentYamlString());
        }

        public void PrintUsage()
        {
            if(string.IsNullOrEmpty(lastWrittenYamlFile))
            {
                Console.WriteLine("No yaml has been written yet, cannot print usage");
                return;
            }
            Console.WriteLine($"");
            Console.WriteLine($"=============================== Launching on Kubernetes ===============================");
            Console.WriteLine($"> kubectl delete all --all -n {K8sNamespace}");
            Console.WriteLine($"> kubectl apply -f {lastWrittenYamlFile}");
            Console.WriteLine($"=======================================================================================");
            Console.WriteLine($"");
        }

        private string GetCurrentProjectPath(string filename = "")
        {
            var workingdir = Directory.GetCurrentDirectory();//get bin folder
            var projectPath = new StringBuilder();
            foreach (var section in workingdir.Split('\\'))
            {
                if (section.Equals("bin")) break;
                projectPath.Append(section).Append('\\');
            }
            return projectPath.Append(filename).ToString();
        }

        private string GetDeploymentYamlString()
        {
            StringBuilder deploymentYamlBuilder = new StringBuilder();
            foreach(var configurator in _configurators)
            {
                foreach(var instanceName in configurator.InstanceNames)
                {
                    deploymentYamlBuilder.Append(BuildDeploymentSection(configurator, instanceName));
                }
            }
            return deploymentYamlBuilder.ToString();
        }

        private string BuildDeploymentSection(IOperatorConfigurator configurator, string instanceName)
        {
            return $@"
kind : Deployment
apiVersion : apps/v1
metadata :
    name : {instanceName}
    namespace : {K8sNamespace}
    labels :
        app : {configurator.OperatorName}
        name : crainst
spec :
    replicas : 1
    selector:
        matchLabels:
            app: {instanceName}
    template :
        metadata :
            name : {instanceName}
            labels:
                app: {configurator.OperatorName}
                name : blacksp
#consider operator name here, could serve for checking logs of all shards at the same time
        spec:
            containers:
            - name : {instanceName}
              image : mdzwart/cra-net2.1:latest
              ports:
              - containerPort: 1500
              env:
              - name: AZURE_STORAGE_CONN_STRING
                value: DefaultEndpointsProtocol=https;AccountName=vertexstore;AccountKey=3BMGVlrXZq8+NE9caC47KDcpZ8X59vvxFw21NLNNLFhKGgmA8Iq+nr7naEd7YuGGz+M0Xm7dSUhgkUN5N9aMLw==;EndpointSuffix=core.windows.net
              args : [""{instanceName}"", 1500] # CRA instance name: {instanceName}, exposed on port 1500
              resources:
        #requests:
        #cpu: ""500m"" #hotfix to prevent two instances on the same node (assuming 1m cpu total)
------------------------";
        }
    }
}
