using BlackSP.Core.Reusability;
using BlackSP.CRA.Endpoints;
using BlackSP.CRA.Events;
using BlackSP.Interfaces.Events;
using BlackSP.Serialization;
using BlackSP.Serialization.Events;
using BlackSP.Serialization.Parallelization;
using CRA.ClientLibrary;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {

        public override Task InitializeAsync(int shardId, ShardingInfo shardingInfo, object vertexParameter)
        {
            Console.Write("Vertex Endpoint Initialization.. ");

            //ZFSerializer.RegisterTypes(); //required for serializer to load serializable types

            var zeroFormatterObjPool = new ParameterlessObjectPool<ZFSerializer>();
            var parallelSerializer = new ProtobufSerializer();//new ParallelSerializer<ZFSerializer>(zeroFormatterObjPool);

            var input = new VertexInputEndpoint<IEvent>(parallelSerializer);
            AddAsyncInputEndpoint($"input", input);
            
            var output = new VertexOutputEndpoint<IEvent>(parallelSerializer);
            AddAsyncOutputEndpoint($"output", output);

            //TODO: remove test crap            
            SpawnLoadGeneratingThread(input, output);
            Console.WriteLine("Done");

            return Task.CompletedTask;
        }

        private void SpawnLoadGeneratingThread(VertexInputEndpoint<IEvent> input, VertexOutputEndpoint<IEvent> output)
        {   //TODO: delete method once served its purpose
            Task.Run(async () =>
            {

                await Task.Delay(10000);//wait 10 seconds for connections to establish etc

                Console.WriteLine("!!! Starting high event load generation !!!");
                Random r = new Random();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                int eventsPerSec = 1000;
                int secondCounter = 0;
                while (true)
                {   //go ham enqueueing events, see how fast we can go..
                    if (!input.IsConnected || !output.IsConnected)
                    {
                        await Task.Delay(100);
                        continue; //dont enqueue while not connected..
                    }

                    for (int i = 0; i < eventsPerSec; i++)
                    {
                        if (!input.IsConnected || !output.IsConnected) { continue; }

                        IEvent next = input.GetNext() ?? new SampleEvent
                        {
                            Key = $"KeyYo {(r.NextDouble() * 1000)}",
                            Value = $"ValueYo{(r.NextDouble() * 1000)}"
                        };
                        output.EnqueueAll(next);
                    }

                    int timeTillSecond = sw.ElapsedMilliseconds < 1000 ? 1000 - (int)sw.ElapsedMilliseconds : 0;
                    await Task.Delay(timeTillSecond);

                    secondCounter++;
                    if(secondCounter % 5 == 0)
                    {
                        DoGC();
                    }

                    if(secondCounter == 30)
                    {
                        eventsPerSec = eventsPerSec >= 100*1000 ? eventsPerSec * 2 : eventsPerSec * 10;
                        secondCounter = 0;
                        Console.WriteLine($"Ramping up to {eventsPerSec} e/s");
                    }

                    sw.Restart();
                }
                Console.WriteLine("how the fuck?");
            });
        }

        private void DoGC()
        {
            GC.Collect();
            Console.WriteLine("force collected garbage");   
        }
    }
}
