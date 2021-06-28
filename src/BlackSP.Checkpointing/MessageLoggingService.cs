using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;
using System.Threading;
using BlackSP.Kernel.Logging;

namespace BlackSP.Checkpointing
{
    public class MessageLoggingService<TMessage> : IMessageLoggingService<TMessage>, ICheckpointable
    {

        public IDictionary<string, int> ReceivedSequenceNumbers => _receivedSequenceNrs;

        private readonly ILogger _logger;
        private readonly IMetricLogger _metricLogger;

        private readonly SemaphoreSlim semaphore;
        /// <summary>
        /// Contains message logs keyed by <b>downstream</b> instance names<br/>
        /// Tuples are (seqNr, msg)
        /// </summary>
        //[ApplicationState]
        private readonly IDictionary<string, LinkedList<(int, TMessage)>> _logs;

        /// <summary>
        /// Contains the received sequence numbers keyed by <b>upstream</b> instance names
        /// </summary>
        [ApplicationState]
        private readonly IDictionary<string, int> _receivedSequenceNrs;

        /// <summary>
        /// Contains the sent sequence numbers keyed by <b>upstream</b> instance names
        /// </summary>
        [ApplicationState]
        private readonly IDictionary<string, int> _sentSequenceNrs;

        public MessageLoggingService(ICheckpointService checkpointService, IMetricLogger metricLogger, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricLogger = metricLogger ?? throw new ArgumentNullException(nameof(metricLogger));
            _ = checkpointService ?? throw new ArgumentNullException(nameof(checkpointService));

            _logs = new Dictionary<string, LinkedList<(int, TMessage)>>();
            _receivedSequenceNrs = new Dictionary<string, int>();
            _sentSequenceNrs = new Dictionary<string, int>();
            semaphore = new SemaphoreSlim(1, 1);
            checkpointService.BeforeCheckpointTaken += () => semaphore.Wait();
            checkpointService.AfterCheckpointTaken += (guid) => semaphore.Release();
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
                _sentSequenceNrs.Add(instanceName, -1); //NOTE: start at -1 (first send seqnr = 0)

            }

            foreach (var instanceName in upstreamInstanceNames)
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
            semaphore.Wait();
            var log = _logs[targetInstanceName];
            int nextSeqNr = GetNextOutgoingSequenceNumberInternal(targetInstanceName);
            _sentSequenceNrs[targetInstanceName] = nextSeqNr;
            log.AddLast((nextSeqNr, message));
            semaphore.Release();
            return nextSeqNr;


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

            var lastSentSeqNr = _sentSequenceNrs[replayInstanceName];
            var lastSeqNrInLog = log.Last?.Value.Item1 ?? -1;
            
            if(lastSeqNrInLog > lastSentSeqNr)
            {
                throw new InvalidOperationException("Error: last sequence number in the log cannot possibly be larger than the last sent sequence number, possibly an implementation error.");
            }

            if (lastSeqNrInLog < lastSentSeqNr) //messages were lost..
            {
                _metricLogger.LostMessages(fromSequenceNr - lastSentSeqNr, replayInstanceName);
                _logger.Warning($"Cannot replay from {fromSequenceNr} till {lastSentSeqNr} to {replayInstanceName}. Log ends at {lastSeqNrInLog}.");
                _sentSequenceNrs[replayInstanceName] = fromSequenceNr - 1; //trick: set the sequencenumbers "back" to prevent downstream from discarding them as duplicates waiting for the lower sequence number that will never come..
            }

            var (seq, _) = current?.Value ?? (-1, default);
            if(seq > fromSequenceNr) //go replay from 10 (problem if seq > 10 (e.g. 11 or 12)) so throw
            {
                throw new ArgumentException($"Cannot replay from sequence number {fromSequenceNr}, first in log is {seq}", nameof(fromSequenceNr));
            }

            while (current != null)
            {
                var (seqNr, msg) = current.Value;
                if (seqNr >= fromSequenceNr)
                {
                    if(msg != null)
                    {
                        //need to replay from here..
                        yield return current.Value;
                    }
                    
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

            semaphore.Wait();
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
                        log.AddFirst((seqNr, default)); //ensure last seqnr is saved in log
                        break;
                    }
                    current = log.First;
                }
                else
                {
                    break; //seNr > sequenceNr so the remainder of the log need not be pruned
                }
            }
            semaphore.Release();
            return pruneCount;            
        }

        public int GetNextOutgoingSequenceNumber(string targetInstance)
        {
            semaphore.Wait();
            var res = GetNextOutgoingSequenceNumberInternal(targetInstance);
            semaphore.Release();
            return res;
        }

        private int GetNextOutgoingSequenceNumberInternal(string targetInstance)
        {
            return _sentSequenceNrs[targetInstance] + 1;


            int nextSeqNr = 0;
            var log = _logs[targetInstance];
            if (log.Any())
            {
                var (lastSeqNr, msg) = log.Last.Value;
                if (msg == null)
                {
                    log.RemoveLast();
                }
                nextSeqNr = lastSeqNr + 1;
            }
            return nextSeqNr;
        }

        public void OnBeforeRestore()
        {
        }

        public void OnAfterRestore()
        {
            foreach (var log in _logs.Values)
            {
                log.Clear();
            }
        }
    }
}
