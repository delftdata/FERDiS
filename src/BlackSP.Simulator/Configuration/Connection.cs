using Nerdbank.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BlackSP.Simulator.Configuration
{
    public class Connection
    {
        public string FromVertexName { get; set; }
        public string FromInstanceName { get; set; }
        public string FromEndpointName { get; set; }
        public int FromShardId { get; set; }
        public int FromShardCount { get; set; }
        public Stream FromStream { get; private set; }

        public string ToVertexName { get; set; }
        public string ToInstanceName { get; set; }
        public string ToEndpointName { get; set; }
        public int ToShardId { get; set; }
        public int ToShardCount { get; set; }
        public Stream ToStream { get; private set; }

        private CancellationTokenSource ResetSource { get; set; }

        public CancellationToken ResetToken => ResetSource.Token;

        public Connection()
        {
            CreateNewStreams();
            CreateNewCTSource();
        }

        public void Reset()
        {
            ResetSource.Cancel();
            CreateNewStreams();
            CreateNewCTSource();
        }

        private void CreateNewStreams()
        {
            var (fromStream, toStream) = FullDuplexStream.CreatePair();
            FromStream = fromStream;
            ToStream = toStream;
        }

        private void CreateNewCTSource()
        {
            ResetSource = new CancellationTokenSource();
        }

    }
}
