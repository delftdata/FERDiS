using BlackSP.Core.Endpoints;
using CRA.ClientLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Endpoints
{
    public class VertexOutputEndpoint : BaseOutputEndpoint, IAsyncShardedVertexOutputEndpoint
    {
        public bool IsConnected { get; set; }

        public async Task ToStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            Console.WriteLine("Starting output channel");
            try
            {
                //CRA invokes current method on a background thread 
                //so just invoke (the blocking) Egress on this thread
                IsConnected = true;
                Egress(stream, otherShardId, token);
                IsConnected = false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception on Egress thread for connection {otherVertex}${otherEndpoint}${otherShardId}");
                Console.WriteLine(e.ToString());
                IsConnected = false;
                throw;
            }
            Console.WriteLine("Stopped output channel");
            token.ThrowIfCancellationRequested();
        }

        public void UpdateShardingInfo(string otherVertex, ShardingInfo shardingInfo)
        {
            Console.WriteLine($"Updating Sharding info in {this.GetType().Name}");
            foreach (var shardId in shardingInfo.AddedShards)
            {
                RegisterRemoteShard(shardId);
            }
            foreach(var shardId in shardingInfo.RemovedShards)
            {
                UnregisterRemoteShard(shardId);
            }
            SetRemoteShardCount(shardingInfo.AllShards.Length);
        }

        public Task ToStreamAsync(Stream stream, string otherVertex, string otherEndpoint, CancellationToken token)
        {
            Console.WriteLine($"Wrong ToStreamAsync in {this.GetType().Name}");
            //We dont need this method as only the overload with shardId is invoked by CRA.
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            { }
        }
    }
}
