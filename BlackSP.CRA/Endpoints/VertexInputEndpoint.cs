using BlackSP.Core.Endpoints;
using BlackSP.Interfaces.Events;
using BlackSP.Interfaces.Serialization;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Endpoints
{
    public class VertexInputEndpoint : BaseInputEndpoint, IAsyncShardedVertexInputEndpoint
    {
        public bool IsConnected { get; set; }

        public VertexInputEndpoint(ISerializer serializer) : base(serializer)
        {

        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            //wraps overload as for input channels we dont care which shard of other vertex it came from
            await FromStreamAsync(stream, otherVertex, $"{otherEndpoint}${otherShardId}", token);
        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, string otherEndpoint, CancellationToken token)
        {
            Console.WriteLine("Starting input channel");
            try
            {
                //CRA invokes this method on a background thread so just invoke Ingress on current thread
                IsConnected = true;
                await Ingress(stream, token);
                IsConnected = false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception on Ingress thread for connection {otherVertex}${otherEndpoint}");
                Console.WriteLine(e.ToString());
                IsConnected = false;
                throw;
            }
            Console.WriteLine("Stopped input channel");
            token.ThrowIfCancellationRequested();
        }

        public void UpdateShardingInfo(string otherVertex, ShardingInfo shardingInfo)
        {
            //No need to care about sharding info on an input endpoint
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {}
        }
    }
}
