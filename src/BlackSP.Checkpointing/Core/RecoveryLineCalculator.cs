using BlackSP.Checkpointing.Extensions;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    public class RecoveryLineCalculator
    {
        /// <summary>
        /// Autofac delegate factory
        /// </summary>
        /// <returns></returns>
        public delegate RecoveryLineCalculator Factory(IEnumerable<MetaData> allCheckpointMetaData);

        private readonly IVertexGraphConfiguration _graphConfiguration;
        private readonly IEnumerable<MetaData> _allCheckpointMetaData;

        public RecoveryLineCalculator(IEnumerable<MetaData> allCheckpointMetaData, IVertexGraphConfiguration graphConfiguration)
        {
            _graphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
            _allCheckpointMetaData = allCheckpointMetaData ?? throw new ArgumentNullException(nameof(allCheckpointMetaData));
        }

        /// <summary>
        /// Calculates a recovery line from the checkpoint metadata the calculator is aware of considering which instances failed.
        /// </summary>
        /// <param name="failedInstances"></param>
        /// <param name="allCheckpointMetaData"></param>
        /// <returns></returns>
        public RecoveryLine CalculateRecoveryLine(bool useFutureCheckpoints, params string[] failedInstances)
        {
            return CalculateRecoveryLine(useFutureCheckpoints, failedInstances.AsEnumerable());
        }

        /// <summary>
        /// Calculates a recovery line from the checkpoint metadata the calculator is aware of considering which instances failed.
        /// </summary>
        /// <param name="failedInstances"></param>
        /// <param name="allCheckpointMetaData"></param>
        /// <returns></returns>
        public RecoveryLine CalculateRecoveryLine(bool useFutureCheckpoints, IEnumerable<string> failedInstances) 
        {
            //prepare datastructure
            var cpStacksPerInstance = _allCheckpointMetaData
                .GroupBy(m => m.InstanceName)
                .Select(group => group.OrderBy(m => m.CreatedAtUtc))
                .Select(sortedGroup => new Stack<MetaData>(sortedGroup))
                .ToList();
            
            if (useFutureCheckpoints)
            {
                //extend datastructure
                AddFutureCheckpoints(cpStacksPerInstance, failedInstances);
            }
            //remove checkpoints that have dependencies (orphan messages)
            EnsureNoOrphansInRecoveryLine(cpStacksPerInstance);
            //construct result object
            IDictionary<string, Guid> recoveryMap = cpStacksPerInstance.WhereStackNonEmpty()
                                                                       .Select(metaStack => metaStack.Peek())
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
        private void AddFutureCheckpoints(IEnumerable<Stack<MetaData>> cpStackPerInstance, IEnumerable<string> excludedInstances)
        {
            var futureMetas = new List<MetaData>();
            foreach (var cpStack in cpStackPerInstance)
            {
                var lastMeta = cpStack.Peek();
                var instanceName = lastMeta.InstanceName;
                if(excludedInstances.Contains(instanceName)) { continue; } //skip over excluded instances

                IDictionary<string, Guid> dependencyDict = new Dictionary<string, Guid>();
                dependencyDict[instanceName] = lastMeta.Id; //Create dependency on more recent checkpoint
                //Get all connections where the "to" end is the current iteration's instanceName, then select all the "from" instanceNames --> all direct upstream instances
                var upstreamInstances = _graphConfiguration.InstanceConnections.Where(t => t.Item2 == instanceName).Select(t => t.Item1);
                foreach (var upstream in upstreamInstances)
                {
                    dependencyDict[upstream] = cpStackPerInstance.Select(s => s.Peek()).First(top => top.InstanceName == upstream).Id;
                }
                var futureMeta = new MetaData(Guid.Empty, dependencyDict, instanceName, DateTime.MaxValue);
                futureMetas.Add(futureMeta); //dont push it on to the stack right away or the next futureMeta may depend on this future meta (that cant happen)
            }

            foreach(var futureMeta in futureMetas)
            {
                cpStackPerInstance.First(s => s.Peek().InstanceName == futureMeta.InstanceName)
                                  .Push(futureMeta);
            }
        }

        /// <summary>
        /// Performs the most critical part of the recovery line calculation, removing checkpoints from the recovery line that depend on eachother.<br/>
        /// Serves to ensure the no-orphan condition.
        /// </summary>
        /// <param name="cpStackPerInstance"></param>
        private void EnsureNoOrphansInRecoveryLine(IEnumerable<Stack<MetaData>> cpStackPerInstance)
        {
        START: //(re)entry point, used when recovery line validity needs to be re-checked after making a change to it
            foreach (var cpStack in cpStackPerInstance.WhereStackNonEmpty())
            {
                var headMeta = cpStack.Peek();
                //select other stack heads except for the one in the current iteration
                var otherMetas = cpStackPerInstance.WhereStackNonEmpty()
                                                    .Select(stack => stack.Peek())
                                                    .Where(m => m.InstanceName != headMeta.InstanceName);
                if (IsAnyReachable(headMeta, otherMetas))
                {
                    cpStack.Pop();
                    goto START; //jump back to retry the loop, recovery line changed
                }
            }
        }

        /// <summary>
        /// Checks whether any otherCandidate can be reached through the dependencies of the candidate.<br/>
        /// This transitively checks, so not only direct dependencies are considered.
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="otherCandidates"></param>
        /// <returns></returns>
        private bool IsAnyReachable(MetaData candidate, IEnumerable<MetaData> otherCandidates)
        {
            //check if othercandidates have dependency on this candidate
            //if yes, return true
            //if no, iterate candidate dependencies and recurse
            //       if nothing left to iterate, return false
            foreach (var potentialDependency in otherCandidates)
            {
                var candidateDeps = candidate.Dependencies;
                if (candidateDeps.ContainsKey(potentialDependency.InstanceName) 
                    && candidateDeps[potentialDependency.InstanceName] == potentialDependency.Id)
                {
                    return true; //there is a dependency on the otherCandidates, therefore it is reachable
                }
            }
            //we passed the previous loop, so there is no direct dependency on an otherCandidate
            //now we recursively try to determine reachability for each dependency of the candidate
            foreach (var pair in candidate.Dependencies)
            {
                var depInstanceName = pair.Key;
                var depCheckpointId = pair.Value;
                var dependencyMeta = _allCheckpointMetaData.First(m => m.Id == depCheckpointId);
                if (IsAnyReachable(dependencyMeta, otherCandidates))
                {
                    return true;
                }
            }
            //finally, no otherCandidate was found to be reachable from candidate. so default to returning false;
            return false;
        }

    }
}
