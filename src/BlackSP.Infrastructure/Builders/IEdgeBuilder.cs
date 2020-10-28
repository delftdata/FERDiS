using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Infrastructure.Builders
{
    public interface IEdgeBuilder
    {
        IVertexBuilder FromVertex { get; }
        string FromEndpoint { get; }

        IVertexBuilder ToVertex { get; }
        string ToEndpoint { get; }

        /// <summary>
        /// Reconfigures the edge builder to create a shuffle connection between the vertices
        /// </summary>
        /// <returns></returns>
        IEdgeBuilder AsShuffle();

        /// <summary>
        /// Bool indicating wether the edge is configured to shuffle (mesh)
        /// </summary>
        /// <returns></returns>
        bool IsShuffle();

        /// <summary>
        /// Reconfigures the edge builder to create a pipeline connection between the vertices
        /// </summary>
        /// <returns></returns>
        IEdgeBuilder AsPipeline();
        
        /// <summary>
        /// Bool indicating wether the edge is configured as a pipeline
        /// </summary>
        /// <returns></returns>
        bool IsPipeline();
    }
}
