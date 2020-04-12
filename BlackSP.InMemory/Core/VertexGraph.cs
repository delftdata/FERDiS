using Autofac;
using BlackSP.InMemory.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.InMemory.Core
{
    public class VertexGraph
    {
        private readonly ILifetimeScope _lifetimeScope;
        private readonly IdentityTable _identityTable;

        public VertexGraph(ILifetimeScope lifetimeScope, IdentityTable identityTable)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _identityTable = identityTable ?? throw new ArgumentNullException(nameof(identityTable));
        }

        public IEnumerable<Task> StartOperating()
        {
            foreach (var instanceName in _identityTable.GetAllInstanceNames())
            {
                yield return StartVertexWithAutoRestart(instanceName);
            }
        }

        private Task StartVertexWithAutoRestart(string instanceName)
        {
            Vertex v = _lifetimeScope.Resolve<Vertex>();
            return Retry(() => v.StartAs(instanceName), int.MaxValue, 1000); 
                
        }

        private static Task<T> Retry<T>(Func<T> func, int retryCount, int delay, TaskCompletionSource<T> tcs = null)
        {
            if (tcs == null)
                tcs = new TaskCompletionSource<T>();
            Task.Run(func).ContinueWith(_original =>
            {
                Console.WriteLine("Vertex thread exited");
                if (_original.IsFaulted)
                {
                    if (retryCount == 0)
                        tcs.SetException(_original.Exception.InnerExceptions);
                    else
                        Console.WriteLine($"Restarting in {delay}ms");
                        Task.Delay(delay).Wait();
                        Retry(func, retryCount - 1, delay, tcs);
                }
                else
                    tcs.SetResult(_original.Result);
            });
            return tcs.Task;
        }

    }
}
