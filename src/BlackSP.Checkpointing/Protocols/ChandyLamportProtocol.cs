using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Diagnostics;

namespace BlackSP.Checkpointing.Protocols
{
    public class ChandyLamportProtocol : ICheckpointable
    {

        [ApplicationState]
        private readonly int placeholder = 0;

        /// <summary>
        /// Autofac delegate factory handle
        /// </summary>
        /// <param name="blockableSource">the source the protocol can block</param>
        /// <returns></returns>
        public delegate ChandyLamportProtocol Factory(IBlockableSource blockableSource);

        private readonly IBlockableSource _blockableSource;
        private readonly ICheckpointService _checkpointingService;
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly ILogger _logger;

        private readonly IEnumerable<string> _allUpstreamConnectionKeys;
        private readonly List<(IEndpointConfiguration, int)> _blockedConnections;
        private IEnumerable<string> BlockedConnectionKeys => _blockedConnections.Select(pair => pair.Item1.GetConnectionKey(pair.Item2));

        public ChandyLamportProtocol(
            IBlockableSource blockableSource,
            ICheckpointService checkpointingService,
            IVertexConfiguration vertexConfiguration,
            ILogger logger)
        {
            _blockableSource = blockableSource; //optional by default..
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _allUpstreamConnectionKeys = vertexConfiguration.InputEndpoints.Where(endpoint => !endpoint.IsControl).SelectMany(endpoint => !endpoint.IsPipeline ? endpoint.GetAllConnectionKeys() : endpoint.GetConnectionKey(vertexConfiguration.ShardId).Yield());
            if(_allUpstreamConnectionKeys.Any())
            {
                _ = _blockableSource ?? throw new ArgumentNullException(nameof(blockableSource)); //argument is required when there are upstream connections
            }
            
            _blockedConnections = new List<(IEndpointConfiguration, int)>();
        }

        /// <summary>
        /// Handles barrier reception
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        /// <returns>bool indicating wether the barrier should be forwarded</returns>
        public async Task<bool> ReceiveBarrier(IEndpointConfiguration origin, int shardId)
        {
            if (!_allUpstreamConnectionKeys.Any()) //no upstream connections? take CP and move on
            {
                await TakeCheckpoint().ConfigureAwait(false);
                return true;
                
            }

            _ = origin ?? throw new ArgumentNullException(nameof(origin));
            return await PerformBarrierBlocking(origin, shardId).ConfigureAwait(false);
        }

        /// <summary>
        /// Utility method that performs barrier blocking + checkpointing + barrier unblocking
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="endpoint"></param>
        /// <param name="shardId"></param>
        /// <returns>bool indicating wether the barrier caused UNblocking</returns>
        private async Task<bool> PerformBarrierBlocking(IEndpointConfiguration origin, int shardId)
        {
            var connectionKey = origin.GetConnectionKey(shardId);
            if (BlockedConnectionKeys.Contains(connectionKey))
            {
                _logger.Warning($"Duplicate barrier received from channel with key: {connectionKey}");
                throw new InvalidOperationException($"Received two barriers from one connection - {origin.RemoteVertexName} at {origin.GetRemoteInstanceName(shardId)} shard {shardId}");
            }
            _logger.Debug($"Received barrier, proceeding to block connection to instance {origin.GetRemoteInstanceName(shardId)}");
            await _blockableSource.Block(origin, shardId).ConfigureAwait(false);
            _blockedConnections.Add((origin, shardId));
            //if all upstream connections are blocked..
            if (BlockedConnectionKeys.Intersect(_allUpstreamConnectionKeys).Count() == _allUpstreamConnectionKeys.Count())
            {
                //take a checkpoint
                await TakeCheckpoint().ConfigureAwait(false);
                //and unblock all connections
                UnblockAllConnections();                
                _logger.Debug($"Unblocked {_allUpstreamConnectionKeys.Count()} upstream connections and request forwarding barrier");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Utility method that unblocks all upstream connections
        /// </summary>
        private void UnblockAllConnections()
        {
            foreach (var (ep, sId) in _blockedConnections)
            {
                _blockableSource.Unblock(ep, sId);
            }
            _blockedConnections.Clear();
        }

        /// <summary>
        /// Utility method that just performs checkpointing
        /// </summary>
        /// <returns></returns>
        private async Task TakeCheckpoint()
        {
            var stopwatch = new Stopwatch();
            _logger.Debug("Received barrier from every upstream, proceeding to take checkpoint");
            stopwatch.Start();
            var newCpId = await _checkpointingService.TakeCheckpoint(_vertexConfiguration.InstanceName).ConfigureAwait(false);
            stopwatch.Stop();
            _logger.Information($"Checkpoint {newCpId} successfully taken in {stopwatch.ElapsedMilliseconds}ms");
        }

        public void OnBeforeRestore()
        { }

        public void OnAfterRestore()
        {
            UnblockAllConnections();
        }
    }
}
