using BlackSP.Infrastructure.Builders.Graph;
using BlackSP.Infrastructure.Models;
using BlackSP.Kernel.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Builders.Application
{
    public class ApplicationBuilder : IApplicationBuilder
    {

        private readonly OperatorVertexGraphBuilderBase _graphBuilder;
        private ILogConfiguration _logConfiguration;

        public ApplicationBuilder(OperatorVertexGraphBuilderBase graphBuilder)
        {
            _graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
            _logConfiguration = new LogConfiguration();
        }

        /// <inheritdoc/>
        public IApplicationBuilder ConfigureCheckpointing()
        {
            throw new NotImplementedException();
        }

        public async Task<IApplication> Build()
        {
            return await _graphBuilder.Build(_logConfiguration).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IApplicationBuilder ConfigureOperators(Action<IOperatorVertexGraphBuilder> builder)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            builder.Invoke(_graphBuilder);
            
            return this;
        }

        /// <inheritdoc/>
        public IApplicationBuilder ConfigureLogging(ILogConfiguration logging)
        {
            _logConfiguration = logging ?? throw new ArgumentNullException(nameof(logging));
            return this;
        }
    }

}
