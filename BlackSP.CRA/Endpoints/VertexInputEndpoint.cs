using BlackSP.Core.Endpoints;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Operators;
using BlackSP.Kernel.Serialization;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Endpoints
{
    public class VertexInputEndpoint : IAsyncShardedVertexInputEndpoint
    {
        public bool IsConnected { get; set; }
        private readonly IInputEndpoint _bspInputEndpoint;

        public VertexInputEndpoint(IInputEndpoint inputEndpoint)
        {
            _bspInputEndpoint = inputEndpoint ?? throw new ArgumentNullException(nameof(inputEndpoint));    
        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            //wraps overload as for input channels, we dont care which shard of other vertex it came from
            await FromStreamAsync(stream, otherVertex, $"{otherEndpoint}${otherShardId}", token).ConfigureAwait(false);
        }

        public async Task FromStreamAsync(Stream stream, string otherVertex, string otherEndpoint, CancellationToken token)
        {
            Console.WriteLine("Starting input channel");
            try
            {
                //CRA invokes this method on a background thread so just invoke Ingress on current thread
                IsConnected = true;
                await _bspInputEndpoint.Ingress(stream, token).ConfigureAwait(false);
                IsConnected = false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception on Ingress thread for connection {otherVertex}${otherEndpoint}");
                Console.WriteLine(e.ToString());
                throw;
            } finally
            {

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
            {
            }
        }
    }
}
