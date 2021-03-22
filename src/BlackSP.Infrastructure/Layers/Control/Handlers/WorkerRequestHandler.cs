using BlackSP.Core.MessageProcessing.Processors;
using BlackSP.Core.Models;
using BlackSP.Core.Monitors;
using BlackSP.Kernel.MessageProcessing;
using BlackSP.Kernel.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackSP.Core.Extensions;
using BlackSP.Core.MessageProcessing.Handlers;
using BlackSP.Infrastructure.Layers.Data;
using BlackSP.Infrastructure.Layers.Control.Payloads;
using BlackSP.Kernel;
using System.Diagnostics;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Extensions;

namespace BlackSP.Infrastructure.Layers.Control.Handlers
{
    /// <summary>
    /// ControlMessage handler dedicated to handling requests on the worker side
    /// </summary>
    public class WorkerRequestHandler : ForwardingPayloadHandlerBase<ControlMessage, WorkerRequestPayload>, IDisposable
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly DataMessageProcessor _processor;
        private readonly ILogger _logger;

        private CancellationTokenSource _ctSource;
        private Task _activeThread;
        private bool disposedValue;

        public WorkerRequestHandler(DataMessageProcessor processor,
                                    IVertexConfiguration vertexConfiguration,  
                                    ILogger logger)
        {            
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        protected override async Task<IEnumerable<ControlMessage>> Handle(WorkerRequestPayload payload)
        {
            _ = payload ?? throw new ArgumentNullException(nameof(payload));

            await PerformRequestedAction(payload).ConfigureAwait(false);

            var response = new ControlMessage();
            response.AddPayload(new WorkerResponsePayload()
            {
                OriginInstanceName = _vertexConfiguration.InstanceName,
                DataProcessActive = _activeThread != null,
                OriginalRequestType = payload.RequestType
            });
            return response.Yield();
        }

        private async Task PerformRequestedAction(WorkerRequestPayload payload)
        {
            var requestType = payload.RequestType;
            try
            {
                Task action = null;
                switch (requestType)
                {
                    case WorkerRequestType.Status:
                        _logger.Verbose("Processing status request");
                        action = Task.CompletedTask;
                        break;
                    case WorkerRequestType.StartProcessing:
                        action = StartDataProcess();
                        break;
                    case WorkerRequestType.StopProcessing:
                        action = StopDataProcess(payload.UpstreamHaltingInstances.OrEmpty(), payload.DownstreamHaltingInstances.OrEmpty());
                        break;
                    default:
                        throw new InvalidOperationException($"Received worker request \"{requestType}\" which is not implemented in {this.GetType()}");
                }
                await action.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Fatal(e, $"Exception in {this.GetType()} while handling request of type \"{requestType}\"");
                throw;
            }
        }

        private Task StartDataProcess()
        {
            if (_activeThread == null)
            {
                _ctSource = new CancellationTokenSource();
                _activeThread = _processor.StartProcess(_ctSource.Token).ContinueWith(LogExceptionIfFaulted, TaskScheduler.Current);
                _logger.Information($"Data processor started by coordinator instruction");
            }
            else
            {
                throw new InvalidOperationException($"Data processor already started, cannot start again");
            }
            return Task.CompletedTask;
        }

        private async Task StopDataProcess(IEnumerable<string> upstreamHaltedInstances, IEnumerable<string> downstreamHaltedInstances)
        {
            if (_activeThread != null)
            {
                _logger.Debug($"Data processor halt instruction received by coordinator");
                var sw = new Stopwatch();
                sw.Start();
                await CancelProcessorThread().ConfigureAwait(false);
                await _processor.Flush(upstreamHaltedInstances, downstreamHaltedInstances).ConfigureAwait(false);
                sw.Stop();
                _logger.Information($"Data processor halt & network flush successful in {sw.ElapsedMilliseconds}ms");
            }
            else
            {
                throw new InvalidOperationException($"Data processor already stopped, cannot stop again");
            }
        }

        private async Task CancelProcessorThread()
        {
            try
            {
                _ctSource.Cancel();
                await _activeThread.ConfigureAwait(false);
                _ctSource.Dispose();
            }
            catch (OperationCanceledException) { /* silence cancellation exceptions, these are expected. */}
            finally
            {
                _activeThread = null;
                _ctSource = null;
            }
        }

        private void LogExceptionIfFaulted(Task t)
        {
            if(t.IsFaulted)
            {
                _logger.Fatal(t.Exception, "DataProcessor thread exited with exception");
            } 
        }

        #region dispose support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _ctSource?.Cancel();
                        _ctSource?.Dispose();
                        _activeThread?.Wait();
                        _activeThread?.Dispose();
                    }
                    catch(Exception) { }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
