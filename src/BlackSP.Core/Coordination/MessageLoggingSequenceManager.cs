using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Coordination
{

    /// <summary>
    /// Manages sequence numbers belonging to checkpoints taken by workers. Used for determining pruning/replay-points
    /// </summary>
    public class MessageLoggingSequenceManager
    {

        /// <summary>
        /// Keyed by checkpoint Id
        /// </summary>
        private readonly IDictionary<Guid, IDictionary<string, int>> _sequenceDict;

        public MessageLoggingSequenceManager()
        {
            _sequenceDict = new Dictionary<Guid, IDictionary<string, int>>();
        }

        /// <summary>
        /// Add a new entry
        /// </summary>
        /// <param name="cpId">identifier of the checkpoint</param>
        /// <param name="sequenceNrs">sequence numbers that were received when the checkpoint was taken</param>
        public void AddCheckpoint(Guid cpId, IDictionary<string, int> sequenceNrs)
        {
            _ = sequenceNrs ?? throw new ArgumentNullException(nameof(sequenceNrs));

            _sequenceDict.Add(cpId, sequenceNrs);
        }


        public IDictionary<string,int> GetPrunableSequenceNumbers(Guid cpId)
        {
            if(!_sequenceDict.ContainsKey(cpId))
            {
                return new Dictionary<string, int>();
            }
            return _sequenceDict[cpId];
        }

    }
}
