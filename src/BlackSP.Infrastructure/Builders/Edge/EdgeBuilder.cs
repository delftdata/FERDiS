using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Builders.Edge
{

    public enum EdgeType
    {
        None,
        Shuffle,
        Pipeline
    }

    public class EdgeBuilder : IEdgeBuilder
    {

        public IVertexBuilder FromVertex { get; private set; }
        public string FromEndpoint { get; private set; }

        public IVertexBuilder ToVertex { get; private set; }
        public string ToEndpoint { get; private set; }

        public EdgeType Type { get; private set; }

        private bool _isBackchannel;

        public EdgeBuilder(IVertexBuilder fromVertex, string fromEndpoint, IVertexBuilder toVertex, string toEndpoint)
        {
            FromVertex = fromVertex ?? throw new ArgumentNullException(nameof(fromVertex));
            FromEndpoint = fromEndpoint ?? throw new ArgumentNullException(nameof(fromEndpoint));

            ToVertex = toVertex ?? throw new ArgumentNullException(nameof(toVertex));
            ToEndpoint = toEndpoint ?? throw new ArgumentNullException(nameof(toEndpoint));

            Type = EdgeType.None;

            _isBackchannel = false;
        }

        public IEdgeBuilder AsPipeline()
        {
            if (FromVertex.InstanceNames.Count != ToVertex.InstanceNames.Count)
            {
                throw new InvalidOperationException("Cannot make a pipeline connection between vertices with different number of shards");
            }

            Type = EdgeType.Pipeline;
            return this;
        }

        public bool IsPipeline()
        {
            return Type == EdgeType.Pipeline;
        }

        public IEdgeBuilder AsShuffle()
        {
            Type = EdgeType.Shuffle;
            return this;
        }

        public bool IsShuffle()
        {
            return Type == EdgeType.Shuffle;
        }

        public IEdgeBuilder AsBackchannel()
        {
            _isBackchannel = true;
            return this;
        }

        public bool IsBackchannel() => _isBackchannel;
    }
}
