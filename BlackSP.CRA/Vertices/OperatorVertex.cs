using Autofac;
using BlackSP.CRA.DI;
using BlackSP.CRA.Endpoints;
using BlackSP.CRA.Events;
using BlackSP.Interfaces.Endpoints;
using BlackSP.Interfaces.Events;
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

        ~OperatorVertex() {
            Dispose(false);
        }

        public override Task InitializeAsync(int shardId, ShardingInfo shardingInfo, object vertexParameter)
        {
            Console.Write("Vertex Initialization.. ");
            VertexParameter param = vertexParameter as VertexParameter ?? throw new ArgumentException($"Argument {nameof(vertexParameter)} was not of type {typeof(VertexParameter)}"); ;
            
            _dependencyContainer = new IoC()
                .RegisterOperator(param.OperatorType)
                .RegisterSerializer(typeof(ProtobufSerializer))
                .RegisterInputEndpoint(typeof(VertexInputEndpoint))
                .RegisterOutputEndpoint(typeof(VertexOutputEndpoint))
                .BuildContainer();
            _vertexLifetimeScope = _dependencyContainer.BeginLifetimeScope();
            
            //TODO: Configure IOC Container (should use types from vertex param?)
            //      + register serializer type
            //      + register operator type
            //      + register logger?
            //      + where to get user delegate for operator from?
            //      + register endpoint input/output (not configurable, per dependency?)
            //TODO: Use Autofac startup to launch operator thread
            //      + keep internal cancellationtokensource in operator
            //      + on exception, cancel + log + throw exception or exit from thread
            //TODO: endpoints should keep an eye on operator cancellationtoken
            //      + join it with external cancellationtoken (CRA)
            //      + exit ingress/egress on cancellation (throw)

            //TODO: Resolve required instances of endpoints
            //      + register them with CRA
            
            var input = _vertexLifetimeScope.Resolve<IAsyncVertexInputEndpoint>();
            AddAsyncInputEndpoint($"input", input);
            
            var output = _vertexLifetimeScope.Resolve<IAsyncVertexOutputEndpoint>();
            AddAsyncOutputEndpoint($"output", output);
            
            //TODO: remove test crap            
            SpawnLoadGeneratingThread(input as VertexInputEndpoint, output as VertexOutputEndpoint);
            SpawnPassthroughThread(input as VertexInputEndpoint, output as VertexOutputEndpoint);
            
            Console.WriteLine("Done");
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if(disposing)
            {
                _dependencyContainer.Dispose();
            }
        }

        private void SpawnPassthroughThread(VertexInputEndpoint input, VertexOutputEndpoint output)
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
        }

        private void SpawnLoadGeneratingThread(VertexInputEndpoint input, VertexOutputEndpoint output)
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
        }
    }
}
