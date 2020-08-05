using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using BlackSP.Simulator.Streams;
using Nerdbank.Streams;

namespace BlackSP.Simulator.Configuration
{
    public class ConnectionTable
    {

        private readonly Dictionary<string, Connection[]> _incomingConnectionDict;
        private readonly Dictionary<string, Stream[]> _incomingStreamDict;

        private readonly Dictionary<string, Connection[]> _outgoingConnectionDict;
        private readonly Dictionary<string, Stream[]> _outgoingStreamDict;

        public ConnectionTable()
        {
            _incomingConnectionDict = new Dictionary<string, Connection[]>();
            _incomingStreamDict = new Dictionary<string, Stream[]>();

            _outgoingConnectionDict = new Dictionary<string, Connection[]>();
            _outgoingStreamDict = new Dictionary<string, Stream[]>();
        }

        public void RegisterConnection(Connection connection)
        {
            var fromKey = GetKey(connection.FromInstanceName, connection.FromEndpointName);
            var toKey = GetKey(connection.ToInstanceName, connection.ToEndpointName);
            if (!_incomingStreamDict.TryGetValue(toKey, out Stream[] inStreams))
            {
                inStreams = new Stream[connection.FromShardCount];
                _incomingStreamDict.Add(toKey, inStreams);
            }
            if (!_outgoingStreamDict.TryGetValue(fromKey, out Stream[] outStreams))
            {
                outStreams = new Stream[connection.ToShardCount];
                _outgoingStreamDict.Add(fromKey, outStreams);
            }

            if (!_incomingConnectionDict.TryGetValue(toKey, out Connection[] inConnections))
            {
                inConnections = new Connection[connection.FromShardCount];
                _incomingConnectionDict.Add(toKey, inConnections);
            }
            if (!_outgoingConnectionDict.TryGetValue(fromKey, out Connection[] outConnections))
            {
                outConnections = new Connection[connection.ToShardCount];
                _outgoingConnectionDict.Add(fromKey, outConnections);
            }
            var (inStream, outStream) = FullDuplexStream.CreatePair();
            //var shareableStream = new ();//new ProducerConsumerStream();// Stream.Synchronized(new ProducerConsumerStream());

            inStreams[connection.FromShardId] = inStream;
            inConnections[connection.FromShardId] = connection;

            outStreams[connection.ToShardId] = outStream;
            outConnections[connection.ToShardId] = connection;

        }

        public Connection[] GetIncomingConnections(string instanceName, string endpointName)
        {
            var key = GetKey(instanceName, endpointName);
            return _incomingConnectionDict[key];
        }

        public Stream[] GetIncomingStreams(string instanceName, string endpointName)
        {
            var key = GetKey(instanceName, endpointName);
            return _incomingStreamDict[key];
        }

        public Connection[] GetOutgoingConnections(string instanceName, string endpointName)
        {
            var key = GetKey(instanceName, endpointName);
            return _outgoingConnectionDict[key];
        }

        public Stream[] GetOutgoingStreams(string instanceName, string endpointName)
        {
            var key = GetKey(instanceName, endpointName);
            return _outgoingStreamDict[key];
        }

        private string GetKey(string instanceName, string endpointName)
        {
            return $"{instanceName}${endpointName}";
        }
    }
}
