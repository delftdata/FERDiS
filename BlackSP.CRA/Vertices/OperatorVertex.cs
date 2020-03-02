using Autofac;
using BlackSP.Core.Endpoints;
using BlackSP.Core.Operators;
using BlackSP.CRA.DI;
using BlackSP.CRA.Endpoints;
using BlackSP.CRA.Events;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Operators;
using BlackSP.Serialization;
using CRA.ClientLibrary;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {
        private IContainer _dependencyContainer;
        private ILifetimeScope _vertexLifetimeScope;
        private IOperator _bspOperator;

        public OperatorVertex()
        {

        }
        
        ~OperatorVertex() {
            Dispose(false);
        }

        private void DoTypeInitialization(IOperatorConfiguration config)
        {
            _dependencyContainer = new IoC()
                .RegisterBlackSPComponents()
                .RegisterCRAComponents()
                .RegisterOperatorConfiguration(config)
                .BuildContainer();
            // register logger?
            
            Console.WriteLine("Dependencies registered");
            
            _vertexLifetimeScope = _dependencyContainer.BeginLifetimeScope();
        }

        public override Task InitializeAsync(int shardId, ShardingInfo shardingInfo, object vertexParameter)
        {
            Console.WriteLine("Installing dependency container");
            IVertexParameter param = vertexParameter as IVertexParameter ?? throw new ArgumentException($"Argument {nameof(vertexParameter)} was not of type {typeof(IVertexParameter)}"); ;

            //TODO: better method name, spins up autofac IoC container
            DoTypeInitialization(param.OperatorConfiguration);

            //TODO: resolve amount required (from vertex parameter)
            var input = _vertexLifetimeScope.Resolve<IAsyncVertexInputEndpoint>();
            AddAsyncInputEndpoint($"input", input);

            //TODO: resolve amount required (from vertex parameter)
            var output = _vertexLifetimeScope.Resolve<IAsyncVertexOutputEndpoint>();
            AddAsyncOutputEndpoint($"output", output);
            
            Type operatorType = param.OperatorType;
            _bspOperator = _vertexLifetimeScope.Resolve(operatorType) as IOperator 
                ?? throw new ArgumentException($"Resolved object with type {operatorType} could not be converted to {nameof(IOperator)}");

            _bspOperator.Start();

            //TODO: remove test crap            
            //SpawnLoadGeneratingThread(input as VertexInputEndpoint, output as VertexOutputEndpoint);
            //SpawnPassthroughThread(input as VertexInputEndpoint, output as VertexOutputEndpoint);
            
            Console.WriteLine("Done");
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        private new void Dispose(bool disposing)
        {
            if(disposing)
            {
                _dependencyContainer.Dispose();
            }
            base.Dispose(disposing);
        }

        /*private void SpawnPassthroughThread(VertexInputEndpoint input, VertexOutputEndpoint output)
        {   //TODO: delete method once served its purpose
            Task.Run(async () =>
            {
                await Task.Delay(10000);//wait 10 seconds for connections to establish etc
                Console.WriteLine("!!! passthrough thread !!!");
                while (true)
                {   //go ham enqueueing events, see how fast we can go..
                    if (!input.IsConnected || !output.IsConnected) { continue; }
                    IEvent next = input.GetNext();
                    if (next != null)
                    {
                        output.EnqueueAll(next);
                    }
                }
            });
        }*/

        /*private void SpawnLoadGeneratingThread(VertexInputEndpoint input, VertexOutputEndpoint output)
        {   //TODO: delete method once served its purpose
            Task.Run(async () =>
            {
                await Task.Delay(10000);//wait 10 seconds for connections to establish etc
                Console.WriteLine("!!! load generating thread !!!");
                Random r = new Random();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                int eventsPerSec = 10000;
                int secondCounter = 0;
                while (true)
                {   //go ham enqueueing events, see how fast we can go..
                    if (!input.IsConnected || !output.IsConnected) { continue; }

                    for (int i = 0; i < eventsPerSec; i++)
                    {
                        IEvent next = new SampleEvent
                        {
                            Key = $"KeyYo {(r.NextDouble() * 1000)}",
                            Value = $"ValueYo{(r.NextDouble() * 1000)}"
                        };
                        output.EnqueueAll(next);
                    }

                    int timeTillSecond = sw.ElapsedMilliseconds < 1000 ? 1000 - (int)sw.ElapsedMilliseconds : 0;
                    await Task.Delay(timeTillSecond);

                    secondCounter++;

                    sw.Restart();
                }
            });
        }*/
    }
}
