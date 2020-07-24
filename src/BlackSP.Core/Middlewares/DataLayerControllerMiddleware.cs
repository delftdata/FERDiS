using BlackSP.Core.Controllers;
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

namespace BlackSP.Core.Middlewares
{
    public class DataLayerControllerMiddleware : IMiddleware<ControlMessage>, IDisposable
    {
        private readonly IVertexConfiguration _vertexConfiguration;
        private readonly DataLayerProcessController _controller;
        private readonly DataLayerProcessMonitor _processMonitor;
        private readonly ILogger _logger;

        private CancellationTokenSource _ctSource;
        private Task _activeThread;
        private bool disposedValue;

        public DataLayerControllerMiddleware(IVertexConfiguration vertexConfiguration, 
                                             DataLayerProcessController processCtrl, 
                                             DataLayerProcessMonitor dataProcessMonitor,
                                             ILogger logger)
        {
            _vertexConfiguration = vertexConfiguration ?? throw new ArgumentNullException(nameof(vertexConfiguration));
            _controller = processCtrl ?? throw new ArgumentNullException(nameof(processCtrl));
            _processMonitor = dataProcessMonitor ?? throw new ArgumentNullException(nameof(dataProcessMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ControlMessage>> Handle(ControlMessage message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if (!message.TryGetPayload<WorkerRequestPayload>(out var payload))
            {
                return new List<ControlMessage>() { message }.AsEnumerable();
            }

            if (!payload.TargetInstanceNames.Contains(_vertexConfiguration.InstanceName))
            {
                return Enumerable.Empty<ControlMessage>();
            }

            try
            {
                Task action = null;
                if (payload.RequestType == WorkerRequestType.StartProcessing)
                {
                    action = StartDataProcess();
                }
                else if (payload.RequestType == WorkerRequestType.StopProcessing)
                {
                    action = StopDataProcess();
                }
                await action.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Warning(e, $"Exception in {this.GetType()} while starting/stopping the data layer");
                throw;
            }

            return Enumerable.Empty<ControlMessage>();
        }

        private Task StartDataProcess()
        {
            if (_activeThread == null)
            {
                _ctSource = new CancellationTokenSource();
                _activeThread = _controller.StartProcess(_ctSource.Token);
                _processMonitor.MarkActive(true);
                _logger.Information($"Data layer was started");
            }
            else
            {
                _logger.Information($"Data layer already started, cannot start again");
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
                _processMonitor.MarkActive(false);
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
