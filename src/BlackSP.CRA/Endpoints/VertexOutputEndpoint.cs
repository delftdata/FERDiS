﻿using BlackSP.Core.Endpoints;
using BlackSP.Kernel.Endpoints;
using BlackSP.Kernel.Events;
using BlackSP.Kernel.Serialization;
using CRA.ClientLibrary;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.CRA.Endpoints
{
    public class VertexOutputEndpoint : IAsyncShardedVertexOutputEndpoint
    {
        private readonly IOutputEndpoint _bspOutputEndpoint;


        public VertexOutputEndpoint(IOutputEndpoint outputEndpoint)
        {
            _bspOutputEndpoint = outputEndpoint ?? throw new ArgumentNullException(nameof(outputEndpoint));
        }

        public async Task ToStreamAsync(Stream stream, string otherVertex, int otherShardId, string otherEndpoint, CancellationToken token)
        {
            try
            {
                //CRA invokes current method on a background thread 
                //so just invoke Egress on this thread
                Console.WriteLine($"Output channel connecting to {otherVertex}${otherEndpoint}${otherShardId} starting");
                _bspOutputEndpoint.RegisterRemoteShard(otherShardId);
                await _bspOutputEndpoint.Egress(stream, otherShardId, token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Output channel connecting to {otherVertex}${otherEndpoint}${otherShardId} exception");
                Console.WriteLine(e.ToString());
                throw;
            }
            finally
            {
                Console.WriteLine($"Output channel connecting to {otherVertex}${otherEndpoint}${otherShardId} stopped");
            }
            token.ThrowIfCancellationRequested();
        }

        public void UpdateShardingInfo(string otherVertex, ShardingInfo shardingInfo)
        {
            Console.WriteLine($"Updating Sharding info in {this.GetType().Name}");
            foreach (var shardId in shardingInfo.AddedShards)
            {
                _bspOutputEndpoint.RegisterRemoteShard(shardId);
            }
            foreach(var shardId in shardingInfo.RemovedShards)
            {
                _bspOutputEndpoint.UnregisterRemoteShard(shardId);
            }
            _bspOutputEndpoint.SetRemoteShardCount(shardingInfo.AllShards.Length);
        }

        public Task ToStreamAsync(Stream stream, string otherVertex, string otherEndpoint, CancellationToken token)
        {
            Console.WriteLine($"Wrong ToStreamAsync in {this.GetType().Name}");
            //We dont need this method as we only use sharded CRA vertices
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