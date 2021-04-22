using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;

namespace BlackSP.Checkpointing
{
    public class MessageLoggingService<TMessage> : IMessageLoggingService<TMessage>
        where TMessage : class, IMessage
    {

        public IDictionary<string, int> ReceivedSequenceNumbers => _receivedSequenceNrs;

        private readonly ILogger _logger;

        private readonly object _lockObj;

        /// <summary>
        /// Contains message logs keyed by <b>downstream</b> instance names<br/>
        /// Tuples are (seqNr, msg)
        /// </summary>
        [ApplicationState]
        private readonly IDictionary<string, LinkedList<(int, TMessage)>> _logs;

        /// <summary>
        /// Contains the received sequence numbers keyed by <b>upstream</b> instance names
        /// </summary>
        [ApplicationState]
        private readonly IDictionary<string, int> _receivedSequenceNrs;

        public MessageLoggingService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logs = new Dictionary<string, LinkedList<(int, TMessage)>>();
            _receivedSequenceNrs = new Dictionary<string, int>();
            _lockObj = new object();
        }

        /// <inheritdoc/>
        public void Initialize(string[] downstreamInstanceNames, string[] upstreamInstanceNames)
        {
            _ = downstreamInstanceNames ?? throw new ArgumentNullException(nameof(downstreamInstanceNames));
            _ = upstreamInstanceNames ?? throw new ArgumentNullException(nameof(upstreamInstanceNames));

            _logger.Information("Initialising message logging service");

            foreach(var instanceName in downstreamInstanceNames)
            {
                _logs.Add(instanceName, new LinkedList<(int, TMessage)>());
            }

            foreach(var instanceName in upstreamInstanceNames)
            {
                _receivedSequenceNrs.Add(instanceName, -1); //NOTE: start at -1 (first send seqnr = 0)
            }
        }

        /// <inheritdoc/>
        public int Append(string targetInstanceName, TMessage message)
        {
            _ = targetInstanceName ?? throw new ArgumentNullException(nameof(targetInstanceName));
            _ = message ?? throw new ArgumentNullException(nameof(message));

            if (!_logs.ContainsKey(targetInstanceName))
            {
                throw new ArgumentException($"Logging service has not been configured for downstream instance with name {targetInstanceName}", nameof(targetInstanceName));
            }
            lock (_lockObj)
            {
                var log = _logs[targetInstanceName];
                int nextSeqNr = 0;

                if (log.Any())
                {
                    var (lastSeqNr, msg) = log.Last.Value;
                    if(msg == null)
                    {
                        log.RemoveLast();
                    }
                    nextSeqNr = lastSeqNr + 1;
                }

                log.AddLast((nextSeqNr, message));
                return nextSeqNr;
            }
                
        }

        /// <inheritdoc/>
        public bool Receive(string originInstance, int sequenceNr)
        {
            if(!_receivedSequenceNrs.ContainsKey(originInstance))
            {
                throw new ArgumentException($"Logging service has not been configured for upstream instance with name {originInstance}", nameof(originInstance));
            }

            var prevSequenceNr = _receivedSequenceNrs[originInstance];

            if(sequenceNr != prevSequenceNr + 1)
            {
                return false; //order is off.. message may not be received
            }

            _receivedSequenceNrs[originInstance] = sequenceNr;
            return true;
        }

        /// <inheritdoc/>
        public void ExpectReplay(string replayInstance, int lastReceivedSequenceNr)
        {
            if (!_receivedSequenceNrs.ContainsKey(replayInstance))
            {
                throw new ArgumentException($"Logging service has not been configured for upstream instance with name {replayInstance}", nameof(replayInstance));
            }
            _receivedSequenceNrs[replayInstance] = lastReceivedSequenceNr;
        }

        /// <inheritdoc/>
        public IEnumerable<(int, TMessage)> Replay(string replayInstanceName, int fromSequenceNr)
        {
            _ = replayInstanceName ?? throw new ArgumentNullException(nameof(replayInstanceName));
            if (!_logs.ContainsKey(replayInstanceName))
            {
                throw new ArgumentException($"Logging service has not been configured for downstream instance with name {replayInstanceName}", nameof(replayInstanceName));
            }

            var log = _logs[replayInstanceName];
            var current = log.First;

            var (seq, _) = current?.Value ?? (-1, null);
            if(seq > fromSequenceNr) //go replay from 10 (problem if seq > 10 (e.g. 11 or 12)) so throw
            {
                throw new ArgumentException($"Cannot replay from sequence number {fromSequenceNr}, first in log is {seq}", nameof(fromSequenceNr));
            }

            while (current != null)
            {
                var (seqNr, msg) = current.Value;
                if (seqNr >= fromSequenceNr && msg != null)
                {
                    //need to replay from here..
                    yield return current.Value;
                }
                current = current.Next;
            }
        }

        /// <inheritdoc/>
        public int Prune(string instanceName, int sequenceNr)
        {
            _ = instanceName ?? throw new ArgumentNullException(nameof(instanceName));
            if (!_logs.ContainsKey(instanceName))
            {
                throw new ArgumentException($"Logging service has not been configured for downstream instance with name {instanceName}", nameof(instanceName));
            }
            
            lock(_lockObj)
            {
                var log = _logs[instanceName];
                var current = log.First;
                var pruneCount = 0;
                while (current != null)
                {
                    var (seqNr, _) = current.Value;

                    if (seqNr <= sequenceNr)
                    {
                        log.RemoveFirst();
                        pruneCount++;

                        if(!log.Any())
                        {
                            log.AddFirst((seqNr, null)); //ensure last seqnr is saved in log
                            break;
                        }
                        current = log.First;
                    }
                    else
                    {
                        break; //seNr > sequenceNr so the remainder of the log need not be pruned
                    }
                }
                return pruneCount;
            }
            
        }

    }
}
