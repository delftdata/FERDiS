using Autofac;
using BlackSP.Simulator.Configuration;
using Serilog;
using System;
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

            _vertexCancellationSources = new Dictionary<string, CancellationTokenSource>();
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
            }
        }

        private async Task StartVertex(string instanceName, int maxRestarts, TimeSpan restartTimeout)
        {
            Vertex v = _lifetimeScope.Resolve<Vertex>();
            while(true)
            {
                var ctSource = new CancellationTokenSource();
                _vertexCancellationSources[instanceName] = ctSource;
                try
                {
                    await v.StartAs(instanceName, ctSource.Token);
                    return;
                } 
                catch(OperationCanceledException)
                {
                    _logger.Warning($"Vertex exited due to cancellation, restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout);
                }
                catch(Exception e)
                {
                    //exited without intent
                    if(maxRestarts-- == 0)
                    {
                        _logger.Fatal($"Vertex exited with exceptions, no restart: exceeded maxRestarts.");
                        throw;
                    }
                    _logger.Warning(e, $"Vertex exited with exceptions, restart in {restartTimeout.TotalSeconds} seconds.");
                    await Task.Delay(restartTimeout).ConfigureAwait(false);
                }
            }             
        }
    }
}
