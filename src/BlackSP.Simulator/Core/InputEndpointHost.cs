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
    public class InputEndpointHost
    {
        private readonly IInputEndpoint _inputEndpoint;
        private readonly ConnectionTable _connectionTable;
        private readonly ILogger _logger;

        public InputEndpointHost(IInputEndpoint inputEndpoint, ConnectionTable connectionTable, ILogger logger)
        {
            _inputEndpoint = inputEndpoint ?? throw new ArgumentNullException(nameof(inputEndpoint));
            _connectionTable = connectionTable ?? throw new ArgumentNullException(nameof(connectionTable));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        /// <summary>
        /// Launches threads for each incoming connection
        /// </summary>
        /// <returns></returns>
        public async Task Start(string instanceName, string endpointName, CancellationToken t)
        {
            var incomingStreams = _connectionTable.GetIncomingStreams(instanceName, endpointName);
            var incomingConnections = _connectionTable.GetIncomingConnections(instanceName, endpointName);

            var hostCTSource = new CancellationTokenSource();
            var linkedCTSource = CancellationTokenSource.CreateLinkedTokenSource(t, hostCTSource.Token);
            var threads = new List<Task>();
            for(var i = 0; i < incomingConnections.Length; i++)
            {
                int shardId = i;
                Stream s = incomingStreams[shardId];
                Connection c = incomingConnections[shardId];
                threads.Add(Task.Run(() => IngressWithRestart(instanceName, endpointName, shardId, 10, TimeSpan.FromSeconds(Constants.KeepAliveTimeoutSeconds * 1.5), linkedCTSource.Token)));
            }

            try
            {
                var exitedThread = await Task.WhenAny(threads).ConfigureAwait(false); //exited means the thread broke out of the restart loop
                await exitedThread.ConfigureAwait(false); //ensure rethrowing any exception on the exited thread
            }
            catch(OperationCanceledException)
            {
                try
                {
                    hostCTSource.Cancel();
                    await Task.WhenAll(threads).ConfigureAwait(false);
                } catch(OperationCanceledException) { /*shh*/}
                
                
                _logger.Debug($"Input endpoint {endpointName} was cancelled and is now resetting streams");
                foreach (var stream in incomingStreams)
                {
                    stream.Dispose();
                }
                foreach(var connection in incomingConnections)
                {
                    _connectionTable.RegisterConnection(connection); //re-register connection to create new streams around a failed instance
                }
                throw;
            } 
            finally
            {
                _logger.Debug($"Exiting input host {endpointName}");
            }
        }

        private async Task IngressWithRestart(string instanceName, string endpointName, int shardId, int maxRestarts, TimeSpan restartTimeout, CancellationToken t)
        {
            //await Task.Delay(2500).ConfigureAwait(false);
            
            while (!t.IsCancellationRequested)
            {
                Connection c = null;
                try
                {

                    t.ThrowIfCancellationRequested();

                    Stream s = _connectionTable.GetIncomingStreams(instanceName, endpointName)[shardId];
                    c = _connectionTable.GetIncomingConnections(instanceName, endpointName)[shardId];
                    
                    _logger.Debug($"Input endpoint {c.ToEndpointName}${shardId} starting ingress");
                    await _inputEndpoint.Ingress(s, c.FromEndpointName, c.FromShardId, t).ConfigureAwait(false);
                    _logger.Debug($"Input endpoint {c.ToEndpointName}${shardId} exiting gracefully");

                }
                catch (OperationCanceledException) when (t.IsCancellationRequested)
                {
                    _logger.Debug($"Input endpoint {c.ToEndpointName}${shardId} exiting due to cancellation");
                    throw;
                }
                catch (Exception e)
                {
                    if (maxRestarts-- == 0)
                    {
                        _logger.Fatal($"Input endpoint {c.ToEndpointName}${shardId} exited with exceptions, no restart: exceeded maxRestarts.");
                        throw;
                    }
                    _logger.Warning(e, $"Input endpoint {c.ToEndpointName}${shardId} exited with {e.GetType()}, restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout, t);
                }
            }
            t.ThrowIfCancellationRequested();
        }
    }
}
