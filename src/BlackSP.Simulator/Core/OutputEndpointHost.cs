using BlackSP.Simulator.Configuration;
using BlackSP.Kernel.Endpoints;
using Nerdbank.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using BlackSP.Core;

namespace BlackSP.Simulator.Core
{
    public class OutputEndpointHost
    {
        private readonly IOutputEndpoint _outputEndpoint;
        private readonly ConnectionTable _connectionTable;
        private readonly ILogger _logger;

        public OutputEndpointHost(IOutputEndpoint outputEndpoint, ConnectionTable connectionTable, ILogger logger)
        {
            _outputEndpoint = outputEndpoint ?? throw new ArgumentNullException(nameof(outputEndpoint));
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Launches threads for each incoming connection
        /// </summary>
        /// <returns></returns>
        public async Task Start(string instanceName, string endpointName, CancellationToken t)
        {
            var outgoingConnections = _connectionTable.GetOutgoingConnections(instanceName, endpointName);

            using var hostCTSource = new CancellationTokenSource();
            using var linkedCTSource = CancellationTokenSource.CreateLinkedTokenSource(t, hostCTSource.Token);
            var threads = new List<Task>();
            for(var i = 0; i < outgoingConnections.Length; i++)
            {
                int shardId = i;
                threads.Add(Task.Run(() => EgressWithRestart(instanceName, endpointName, shardId, 10, TimeSpan.FromSeconds(10), linkedCTSource.Token)));
            }

            try
            {
                var exitedThread = await Task.WhenAny(threads).ConfigureAwait(false);
                await exitedThread.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    hostCTSource.Cancel();
                    await Task.WhenAll(threads).ConfigureAwait(false);
                }
                catch (OperationCanceledException e) { /*shh*/}

                _logger.Debug($"Output endpoint {endpointName} was cancelled and is now resetting streams");
                foreach (var connection in outgoingConnections)
                {
                    connection.Reset(); //reset to cancel receiving endpoint and reset streams
                }

                throw;
            }
            finally
            {
                _logger.Debug($"Exiting output host {endpointName}");
            }
        }

        private async Task EgressWithRestart(string instanceName, string endpointName, int shardId, int maxRestarts, TimeSpan restartTimeout, CancellationToken t)
        {
            //await Task.Delay(2500).ConfigureAwait(false);

            while (!t.IsCancellationRequested)
            {
                Connection c = null;
                try
                {
                    t.ThrowIfCancellationRequested();

                    c = _connectionTable.GetOutgoingConnections(instanceName, endpointName)[shardId];
                    using var callerOrResetSource = CancellationTokenSource.CreateLinkedTokenSource(t, c.ResetToken);
                    _logger.Debug($"Output endpoint {c.FromEndpointName}${shardId} starting egress");
                    await _outputEndpoint.Egress(c.ToStream, c.ToEndpointName, c.ToShardId, callerOrResetSource.Token).ConfigureAwait(false);
                    _logger.Debug($"Output endpoint {c.FromEndpointName}${shardId} exiting gracefully");
                }
                catch (OperationCanceledException) when (t.IsCancellationRequested)
                {
                    _logger.Debug($"Output endpoint {c.FromEndpointName}${shardId} exiting due to cancellation");
                    throw;
                }
                catch (Exception e)
                {
                    if (maxRestarts-- == 0)
                    {
                        _logger.Fatal($"Output endpoint {c.FromEndpointName}${shardId} exited with exceptions, no restart: exceeded maxRestarts.");
                        throw;
                    }
                    _logger.Warning($"Output endpoint {c.FromEndpointName}${shardId} exited with {e.GetType()}, restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout, t).ConfigureAwait(false);
                }
            }
            t.ThrowIfCancellationRequested();
        }
    }
}
