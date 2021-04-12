using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Checkpointing
{
    public interface IMessageLoggingService<TMessage>
    {
        /// <summary>
        /// Received sequence number map
        /// </summary>
        IDictionary<string, int> ReceivedSequenceNumbers { get; }


        /// <summary>
        /// Creates internal log objects for each provided downstream instance
        /// </summary>
        /// <param name="downstreamInstanceNames"></param>
        void Initialize(string[] downstreamInstanceNames, string[] upstreamInstanceNames);

        /// <summary>
        /// Adds the message to log of a downstream instance
        /// </summary>
        /// <param name="targetInstances"></param>
        /// <param name="message"></param>
        /// <returns>The sequence number associated with the newly added message</returns>
        int Append(string targetInstance, TMessage message);
        
        /// <summary>
        /// Receive a new sequence number. May request dropping if a replay is expected
        /// </summary>
        /// <param name="originInstance"></param>
        /// <param name="sequenceNr"></param>
        /// <returns>Whether the message may be received</returns>
        bool Receive(string originInstance, int sequenceNr);

        /// <summary>
        /// Gets an enumerable for message replay given a <b>downstream</b> instance and a sequenceNr to replay from
        /// </summary>
        /// <param name="replayInstanceName"></param>
        /// <param name="fromSequenceNr"></param>
        /// <returns></returns>
        IEnumerable<(int, TMessage)> Replay(string replayInstanceName, int fromSequenceNr);

        /// <summary>
        /// Primes the message logging service for an upstream instance to perform a log replay<br/>
        /// After calling this method the service will request the dropping of messages through the Receive method
        /// </summary>
        /// <param name="replayInstance"></param>
        /// <param name="lastReceivedSequenceNr">Receive will expect the next sequence number to be this number+1</param>
        /// <returns></returns>
        void ExpectReplay(string replayInstanceName, int lastReceivedSequenceNr);

        /// <summary>
        /// Prunes a log for one particular <b>downstream</b> instance.
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="sequenceNr">Last sequence number that will never have to be replayed again</param>
        /// <returns></returns>
        int Prune(string instanceName, int sequenceNr);

    }
}
