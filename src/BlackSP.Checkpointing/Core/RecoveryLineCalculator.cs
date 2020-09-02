using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    public class RecoveryLineCalculator
    {

        IVertexGraphConfiguration _graphConfiguration;

        public RecoveryLineCalculator(IVertexGraphConfiguration graphConfiguration)
        {
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
        }

        public RecoveryLine CalculateRecoveryLine(IEnumerable<string> failedInstances, IEnumerable<MetaData> allCheckpointMetaData) 
        {
            var cpStacksPerInstance = allCheckpointMetaData
                .GroupBy(m => m.InstanceName)
                .Select(group => group.OrderBy(m => m.CreatedAtUtc))
                .Select(sortedGroup => new Stack<MetaData>(sortedGroup));

            AddFutureCheckpointsForEachInstance(cpStacksPerInstance, failedInstances);
            
            //TODO: pop off metas that depend on other stack tops
            //      untill no dependents left

            //iterate all and check if..
            //checkpoint ID does exist in any other checkpoint
            
            IDictionary<string, Guid> recoveryMap = cpStacksPerInstance.Select(metaStack => metaStack.Peek()) //select the remaining checkpoint atop each stack
                .ToDictionary(meta => meta.InstanceName, meta => meta.Id);

            return new RecoveryLine(recoveryMap);
        }

        /// <summary>
        /// On each of the stacks a future (fake) checkpoint is added that represents the still existing state in the running instance.<br/>
        /// By adding these prior to a recovery line calculation the still existing state may be chosen to be 'restored', resulting in 
        /// effectively no checkpoint restore in that case.
        /// </summary>
        /// <param name="cpStackPerInstance">expected stack per instance</param>
        /// <param name="excludedInstances"></param>
        private void AddFutureCheckpointsForEachInstance(IEnumerable<Stack<MetaData>> cpStackPerInstance, IEnumerable<string> excludedInstances)
        {
            foreach (var cpStack in cpStackPerInstance)
            {
                var instanceName = cpStack.Peek().InstanceName;
                if (excludedInstances.Contains(instanceName))
                {
                    continue; //Skip excluded instances
                }
                IDictionary<string, Guid> dependencyDict = new Dictionary<string, Guid>();
                dependencyDict[instanceName] = cpStack.Peek().Id; //Create dependency on previous checkpoint
                //Get all connections where the "to" end is the current iteration's instanceName, then select all the "from" instanceNames --> all direct upstream instances
                var upstreamInstances = _graphConfiguration.InstanceConnections.Where(t => t.Item2 == instanceName).Select(t => t.Item1);
                foreach (var upstream in upstreamInstances)
                {
                    //TODO: verify correctness of statement below
                    dependencyDict[upstream] = cpStackPerInstance.First(stack => stack.Peek().InstanceName == upstream).Peek().Id;
                }
                var futureMeta = new MetaData(Guid.Empty, dependencyDict, instanceName, DateTime.MaxValue);
                cpStack.Push(futureMeta);
            }
        }
    }
}
