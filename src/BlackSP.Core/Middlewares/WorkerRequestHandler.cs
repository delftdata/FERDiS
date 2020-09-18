using BlackSP.Core.Processors;
using BlackSP.Core.Models;
using BlackSP.Core.Models.Payloads;
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

namespace BlackSP.Core.Middlewares
{
    /// <summary>
    /// ControlMessage handler dedicated to handling requests on the worker side
    /// </summary>
    public class WorkerRequestHandler : IMiddleware<ControlMessage>, IDisposable
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly DataMessageProcessor _processor;
        private readonly ConnectionMonitor _connectionMonitor;
        private readonly ILogger _logger;

        private CancellationTokenSource _ctSource;
        private Task _activeThread;
        private bool upstreamFullyConnected;
        private bool downstreamFullyConnected;
        private bool disposedValue;

        public WorkerRequestHandler(DataMessageProcessor processor,
                                    ConnectionMonitor connectionMonitor,
                                    IVertexConfiguration vertexConfiguration,  
                                    ILogger logger)
        {            
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _connectionMonitor = connectionMonitor ?? throw new ArgumentNullException(nameof(connectionMonitor));
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            upstreamFullyConnected = downstreamFullyConnected = false;
            _connectionMonitor.OnConnectionChange += ConnectionMonitor_OnConnectionChange;
        }

        public async Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if (!message.TryGetPayload<WorkerRequestPayload>(out var payload))
            {
                return new List<ControlMessage>() { message }.AsEnumerable();
            }

            await PerformRequestedAction(payload.RequestType).ConfigureAwait(false);

            var response = new ControlMessage();
            response.AddPayload(new WorkerResponsePayload()
            {
                OriginInstanceName = _vertexConfiguration.InstanceName,
                UpstreamFullyConnected = upstreamFullyConnected,
                DownstreamFullyConnected = downstreamFullyConnected,
                DataProcessActive = _activeThread != null,
                OriginalRequestType = payload.RequestType
            });
            return response.Yield();
        }

        private async Task PerformRequestedAction(WorkerRequestType requestType)
        {
            try
            {
                Task action = null;
                switch (requestType)
                {
                    case WorkerRequestType.Status:
                        action = Task.CompletedTask;
                        break;
                    case WorkerRequestType.StartProcessing:
                        action = StartDataProcess();
                        break;
                    case WorkerRequestType.StopProcessing:
                        action = StopDataProcess();
                        break;
                    default:
                        throw new InvalidOperationException($"Worker received instruction \"{requestType}\" which is not implemented in {this.GetType()}");
                }
                await action.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Warning(e, $"Exception in {this.GetType()} while handling request of type \"{requestType}\"");
                throw;
            }
        }

        private void ConnectionMonitor_OnConnectionChange(ConnectionMonitor sender, ConnectionMonitorEventArgs e)
        {
            upstreamFullyConnected = e.UpstreamFullyConnected;
            downstreamFullyConnected = e.DownstreamFullyConnected;
        }

        private Task StartDataProcess()
        {
            if (_activeThread == null)
            {
                _ctSource = new CancellationTokenSource();
                _activeThread = _processor.StartProcess(_ctSource.Token);
                _logger.Information($"Data processor was started");
            }
            else
            {
                _logger.Information($"Data processor already started, cannot start again");
            }
            return Task.CompletedTask;
        }

        private async Task StopDataProcess()
        {
            if (_activeThread != null)
            {
                await CancelProcessingAndResetLocally().ConfigureAwait(false);
                _logger.Information($"Data layer was stopped");
            }
            else
            {
                _logger.Information($"Data layer already stopped, cannot stop again");
            }
        }

        private async Task CancelProcessingAndResetLocally()
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

        #region dispose support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(_activeThread != null)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        CancelProcessingAndResetLocally();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DataLayerControllerMiddleware()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
