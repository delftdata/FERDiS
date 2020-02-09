using BlackSP.Core.Reusability;
using BlackSP.Core.Serialization;
using BlackSP.Core.Serialization.Parallelization;
using BlackSP.CRA.Endpoints;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.CRA.Vertices
{
    public class OperatorVertex : ShardedVertexBase
    {

        public override Task InitializeAsync(int shardId, ShardingInfo shardingInfo, object vertexParameter)
        {
            Console.Write("Vertex Endpoint Initialization.. ");
            var input = new VertexInputEndpoint();
            AddAsyncInputEndpoint($"input", input);

            var apexObjectPool = new ParameterlessObjectPool<ApexEventSerializer>();
            var ser = new ParallelSerializer<ApexEventSerializer>(apexObjectPool);
            var output = new VertexOutputEndpoint(ser);
            AddAsyncOutputEndpoint($"output", output);


            //TODO: remove test crap            
            SpawnLoadGeneratingThread(input, output);
            Console.WriteLine("Done");

            return Task.CompletedTask;
        }

        private void SpawnLoadGeneratingThread(VertexInputEndpoint input, VertexOutputEndpoint output)
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

                        output.EnqueueAll(new SampleEvent($"KeyYo {(r.NextDouble() * 1000)}", $"ValueYo{(r.NextDouble() * 1000)}"));
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
