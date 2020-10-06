using BlackSP.Core.Models;
using BlackSP.Core.Processors;
using BlackSP.Kernel;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Infrastructure.Layers.Data
{
    /// <summary>
    /// Controls the message passing between underlying components<br/>
    /// Takes data messages from the source, passes to pipeline one at a time and passes results to dispatcher
    /// </summary>
    public class DataMessageProcessor : SingleSourceProcessorBase<DataMessage>
    {
        private readonly ISource<DataMessage> _source;
        private readonly IDispatcher<DataMessage> _dispatcher;
        private readonly ILogger _logger;

        public DataMessageProcessor(
            ISource<DataMessage> source,
            IPipeline<DataMessage> pipeline,
            IDispatcher<DataMessage> dispatcher,
            ILogger logger) : base(source, pipeline, dispatcher, logger)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Flush(IEnumerable<string> upstreamInstancesThatHalted)
        {
            var upstreamFlush = _source.Flush(upstreamInstancesThatHalted);
            var downstreamFlush = _dispatcher.Flush();
            await Task.WhenAll(upstreamFlush, downstreamFlush).ConfigureAwait(false);
            _logger.Fatal("DataMessageProcessor flushed input and output successfully"); //TODO: verbose/debug level
        }
    }
}
