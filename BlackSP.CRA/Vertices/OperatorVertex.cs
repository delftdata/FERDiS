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

            //var input2 = new VertexInputEndpoint();
            //AddAsyncInputEndpoint($"input2", input2);

            var output = new VertexOutputEndpoint();
            AddAsyncOutputEndpoint($"output", output);

            //var output2 = new VertexOutputEndpoint();
            //AddAsyncOutputEndpoint($"output2", output2);
            
            SpawnLoadGeneratingThread(input, output);

            Console.WriteLine("Done");

            //_operator.RegisterInputEndpoint(input);
            return Task.CompletedTask;
        }

        private void SpawnLoadGeneratingThread(VertexInputEndpoint input, VertexOutputEndpoint output)
        {
            //TODO: RAMPUP!!
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

                        output.EnqueueAll(new SampleEvent("KeyYo" + (r.NextDouble() * 1000), "ValueYo" + (r.NextDouble() * 1000)));
                    }

                    int timeTillSecond = sw.ElapsedMilliseconds < 1000 ? 1000 - (int)sw.ElapsedMilliseconds : 0;
                    await Task.Delay(timeTillSecond);

                    secondCounter++;
                    if(secondCounter % 10 == 0)
                    {
                        DoGC();
                    }

                    if(secondCounter == 60)
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
            //GC.Collect();
            //Console.WriteLine("force collected garbage");   
        }
    }
}
