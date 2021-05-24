using BlackSP.Infrastructure.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlackSP.CRA.Kubernetes
{
    public class KubernetesDeploymentUtility
    {
        private ICollection<IVertexBuilder> _configurators;
        private string lastWrittenYamlFile;

        private string DockerHubImageName => Environment.GetEnvironmentVariable("CRA_WORKER_DOCKER_IMAGE");

        public KubernetesDeploymentUtility()
        {}
        
        public KubernetesDeploymentUtility(ICollection<IVertexBuilder> configurators)
        {
            _configurators = configurators ?? throw new ArgumentNullException(nameof(configurators));
            lastWrittenYamlFile = string.Empty;
        }

        public KubernetesDeploymentUtility With(ICollection<IVertexBuilder> configurators)
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
            Console.WriteLine($"> kubectl apply -f {lastWrittenYamlFile}");
            Console.WriteLine($"> kubectl delete -f {lastWrittenYamlFile}");
            Console.WriteLine($"> kubectl logs -l instance=crainst01 -f"); //watching a particular instance/shard
            Console.WriteLine($"> kubectl logs -l operator=source01 -f"); //watching logs of all shards of an operator
            Console.WriteLine($"=======================================================================================");
            Console.WriteLine($"");
            Console.WriteLine($"================================= Launching on Docker =================================");
            Console.Write($"> docker run --env AZURE_STORAGE_CONN_STRING=\"{Environment.GetEnvironmentVariable("AZURE_STORAGE_CONN_STRING")}\"");
            Console.Write($" {DockerHubImageName}"); //container spec
            Console.WriteLine($" crainst01 1500"); //commandline args
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

        private string BuildDeploymentSection(IVertexBuilder builder, string instanceName)
        {
            return $@"kind: StatefulSet
apiVersion: apps/v1
metadata:
    namespace: default
    name: {instanceName}
    labels:
        operator: {builder.VertexName}
        instance: {instanceName}
spec:
    replicas: 1
    serviceName: {instanceName}
    selector:
        matchLabels:
            instance: {instanceName}
    template:
        metadata:
            name: {instanceName}
            labels:
                operator: {builder.VertexName}
                instance: {instanceName}
        spec:
            containers:
            - name: {instanceName}
              image: {DockerHubImageName ?? throw new InvalidOperationException("Missing CRA_WORKER_DOCKER_IMAGE environment variable")}
              ports:
              - containerPort: 1500
              env:
{GetEnvironmentSection()}
              args: [""{instanceName}"", ""1500""] #CRA instance name {instanceName}, exposed on port 1500
              resources: 
                requests:
                    cpu: 500m #best-effort attempt at spreading instances over cpus
---
";
        }

        private string GetEnvironmentSection()
        {
            var keysToInsert = Environment.GetEnvironmentVariable("CRA_ENVIRONMENT_VARIABLE_KEYS_TO_COPY");
            var sb = new StringBuilder();

            foreach (var dirtyKey in keysToInsert.Split(','))
            {
                var key = dirtyKey.Trim();
                var value = Environment.GetEnvironmentVariable(key);
                sb.Append("              - name: ").Append(key).AppendLine()
                  .Append("                value: ").Append('"').Append(value).Append('"').AppendLine();
            }
            return sb.Remove(sb.Length -1, 1).ToString();
        }
    }
}
