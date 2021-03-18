using Autofac;
using BlackSP.Simulator.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Simulator.Core
{
    public class VertexGraph
    {
        private readonly ILifetimeScope _lifetimeScope;
        private readonly IdentityTable _identityTable;
        private readonly IDictionary<string, CancellationTokenSource> _vertexCancellationSources;
        private readonly ILogger _logger;

        public VertexGraph(ILifetimeScope lifetimeScope, IdentityTable identityTable, ILogger logger)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _vertexCancellationSources = new ConcurrentDictionary<string, CancellationTokenSource>();
        }

        public IEnumerable<Task> StartAllVertices(int maxRestarts, TimeSpan restartTimeout)
        {


            foreach (var instanceName in _identityTable.GetAllInstanceNames())
            {
                yield return StartVertex(instanceName, maxRestarts, restartTimeout);
            }
        }

        public void KillVertex(string instanceName)
        {
            var source = _vertexCancellationSources[instanceName];
            if (source != null)
            {
                source.Cancel();
                _vertexCancellationSources.Remove(instanceName);
            }
        }

        private async Task StartVertex(string instanceName, int maxRestarts, TimeSpan restartTimeout)
        {
            Vertex v = _lifetimeScope.Resolve<Vertex>();

            while (true)
            {
                var ctSource = new CancellationTokenSource();
                _vertexCancellationSources.Add(instanceName, ctSource);
                try
                {
                    await Task.Run(() => v.StartAs(instanceName, ctSource.Token)).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    _logger.Warning($"Vertex exited due to cancellation, restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    //exited without intent
                    if (maxRestarts-- == 0)
                    {
                        _logger.Fatal($"Vertex exited with exceptions, no restart: exceeded maxRestarts.");
                        throw;
                    }
                    _logger.Warning(e, $"Vertex exited with exceptions, restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout).ConfigureAwait(false);
                }
            }


            //return Task.Run(async () =>
            //{});//.ContinueWith(t => { Console.WriteLine("SIMULATION PROBLEM " + in); }, TaskScheduler.Current);
        }
    }
}
