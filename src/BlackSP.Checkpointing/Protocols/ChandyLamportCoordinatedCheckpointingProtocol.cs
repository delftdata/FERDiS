using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackSP.Checkpointing.Protocols
{
    public class ChandyLamportCoordinatedCheckpointingProtocol
    {

        /// <summary>
        /// Autofac delegate factory handle
        /// </summary>
        /// <param name="blockableSource">the source the protocol can block</param>
        /// <returns></returns>
        public delegate ChandyLamportCoordinatedCheckpointingProtocol Factory(IBlockableSource blockableSource);

        private readonly IBlockableSource _blockableSource;
        private readonly ICheckpointService _checkpointingService;


        private readonly IEnumerable<string> _allUpstreamConnectionKeys;


        public ChandyLamportCoordinatedCheckpointingProtocol(
            IBlockableSource blockableSource,
            ICheckpointService checkpointingService,
            IVertexConfiguration vertexConfiguration)
        {
            _blockableSource = blockableSource ?? throw new ArgumentNullException(nameof(blockableSource));
            _checkpointingService = checkpointingService ?? throw new ArgumentNullException(nameof(checkpointingService));
            _ = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));

            _allUpstreamConnectionKeys = vertexConfiguration.InputEndpoints.Where(endpoint => !endpoint.IsControl).SelectMany(endpoint => !endpoint.IsPipeline ? endpoint.GetAllConnectionKeys() : endpoint.GetConnectionKey(vertexConfiguration.ShardId).Yield());

        }

        /// <summary>
        /// Handles barrier reception
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        /// <returns>bool indicating wether the barrier should be forwarded</returns>
        public bool ReceiveBarrier(IEndpointConfiguration origin, int shardId)
        {
            throw new NotImplementedException();
        }

    }
}
