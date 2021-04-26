using BlackSP.Checkpointing.Models;
using BlackSP.Kernel.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;


namespace BlackSP.Checkpointing.UnitTests.Recovery
{
    internal class CheckpointingInstanceGraphTestUtility
    {
        internal List<string> instanceNames;
        internal List<Tuple<string, string>> instanceConnections;
        internal Dictionary<string, Stack<MetaData>> instanceCheckpoints;

        internal CheckpointingInstanceGraphTestUtility()
        {
            instanceNames = new List<string>();
            instanceConnections = new List<Tuple<string, string>>();
            instanceCheckpoints = new Dictionary<string, Stack<MetaData>>();

        }

        internal IEnumerable<MetaData> GetAllCheckpointMetaData()
        {
            return instanceCheckpoints.SelectMany(pair => pair.Value.AsEnumerable());
        }

        internal IVertexGraphConfiguration GetGraphConfig()
        {
            var graphConfigMock = new Mock<IVertexGraphConfiguration>();
            graphConfigMock.Setup(config => config.InstanceNames).Returns(instanceNames);
            graphConfigMock.Setup(config => config.InstanceConnections).Returns(instanceConnections);
            return graphConfigMock.Object;
        }

        internal void AddConnection(string from, string to)
        {
            instanceConnections.Add(Tuple.Create(from, to));
        }

        internal void AddInstance(string instanceName)
        {
            instanceNames.Add(instanceName);
            instanceCheckpoints.Add(instanceName, new Stack<MetaData>());
        }

        /// <summary>
        /// Adds a checkpoint to the instance by name, automatically sets up dependencies according to known connections
        /// </summary>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        internal Guid AddCheckpoint(string instanceName, bool skipDependencies = false)
        {
            var dependencies = new Dictionary<string, Guid>();
            foreach(var upstreamInstanceName in instanceConnections.Where(c => !skipDependencies && c.Item2 == instanceName).Select(c => c.Item1))
            {
                var upstreamCheckpoints = instanceCheckpoints[upstreamInstanceName];
                if(upstreamCheckpoints.Any())
                {
                    dependencies.Add(upstreamInstanceName, upstreamCheckpoints.Peek().Id);
                }
            }
            var cpId = Guid.NewGuid();
            instanceCheckpoints[instanceName].Push(new MetaData(cpId, dependencies, instanceName, DateTime.Now.AddMinutes(-9)));
            return cpId;
        }

        /// <summary>
        /// Pretends to force a checkpoint, use this to create checkpoints that were forced<br/> 
        /// (i.e. do not depend on upstream latest checkpoint, but rather second latest)
        /// </summary>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        internal Guid ForceCheckpoint(string instanceName, params string[] instanceDependenciesToSkip)
        {
            var dependencies = new Dictionary<string, Guid>();
            foreach (var upstreamInstanceName in instanceConnections.Where(c => c.Item2 == instanceName).Select(c => c.Item1))
            {
                var upstreamCheckpoints = instanceCheckpoints[upstreamInstanceName];
                if(instanceDependenciesToSkip.Contains(upstreamInstanceName))
                {
                    //take second last CP (or none)
                    if (upstreamCheckpoints.Count() > 1)
                    {
                        var last = upstreamCheckpoints.Pop();
                        dependencies.Add(upstreamInstanceName, upstreamCheckpoints.Peek().Id);
                        upstreamCheckpoints.Push(last);
                    }
                } 
                else
                {
                    if (upstreamCheckpoints.Any())
                    {
                        dependencies.Add(upstreamInstanceName, upstreamCheckpoints.Peek().Id);
                    }
                }

                
            }
            var cpId = Guid.NewGuid();
            instanceCheckpoints[instanceName].Push(new MetaData(cpId, dependencies, instanceName, DateTime.Now.AddMinutes(-9)));
            return cpId;
        }
    }
}
