using BlackSP.Kernel.Checkpointing;
using BlackSP.Kernel.Configuration;
using BlackSP.Kernel.MessageProcessing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Checkpointing.Protocols
{
    public class HMNRProtocol : ICheckpointable
    {
        public string InstanceName => currentInstance;

        private readonly ILogger _logger;

        private string currentInstance;

        private string[] allInstances;

        private IDictionary<string, int> nameToIndexDict;

        private int i => nameToIndexDict[currentInstance];

        /// <summary>
        /// 
        /// </summary>
        [ApplicationState]
        private int[] clock;

        /// <summary>
        /// 
        /// </summary>
        [ApplicationState]
        private int[] ckpt;

        /// <summary>
        /// 
        /// </summary>
        [ApplicationState]
        private bool[] taken;

        /// <summary>
        /// 
        /// </summary>
        [ApplicationState]
        private bool[] sent_to;

        /// <summary>
        /// 
        /// </summary>
        [ApplicationState]
        private int[] min_to;

        public HMNRProtocol(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            nameToIndexDict = new Dictionary<string, int>();

        }

        public void InitializeClocks(string currentInstanceName, string[] allInstanceNames)
        {
            currentInstance = currentInstanceName ?? throw new ArgumentNullException(nameof(currentInstanceName));
            allInstances = allInstanceNames ?? throw new ArgumentNullException(nameof(allInstanceNames));
            
            if (!allInstanceNames.Any())
            {
                throw new ArgumentException("Array requires at least one element", nameof(allInstanceNames));
            }

            int c = allInstances.Length;
            clock = new int[c]; //defaults to zeros
            ckpt = new int[c]; //defaults to zeros
            min_to = new int[c]; //defaults to zeros
            sent_to = new bool[c]; //defaults to falses
            taken = new bool[c]; //defaults to falses

            int i = 0;
            foreach (var instance in allInstanceNames)
            {                    
                taken[i] = currentInstanceName != instance;

                nameToIndexDict.Add(instance, i);
                i++;
            }
        }

        /// <summary>
        /// Handle reception of a message clock from a neighbouring instance
        /// </summary>
        /// <param name="mClock"></param>
        /// <returns></returns>
        public bool CheckCheckpointCondition(string originInstance, int[] mclock, int[] mckpt, bool[] mtaken)
        {
            var j = nameToIndexDict[originInstance];
            foreach(int k in nameToIndexDict.Values)
            {
                //clock check detects non-causal z-patterns
                bool clockCond = sent_to[k] && min_to[k] < mclock[j] && Math.Max(clock[k], mclock[k]) < mclock[j];
                //ckpt check detects checkpoints on z-cycles
                bool ckptCond = mtaken[i] && (mckpt[i] == ckpt[i]);

                if(clockCond || ckptCond)
                {
                    return true;
                }
            }
            return false;
        }

        public void BeforeDeliver(string originInstance, int[] mclock, int[] mckpt, bool[] mtaken)
        {
            var j = nameToIndexDict[originInstance];

            clock[i] = Math.Max(clock[i], mclock[j]);

            foreach(int k in nameToIndexDict.Values)
            {
                if(k == i) { continue; }
                clock[k] = Math.Max(clock[k], mclock[k]);
                if(mckpt[k] > ckpt[k])
                {
                    ckpt[k] = mckpt[k];
                    taken[k] = mtaken[k];
                }
                else if(mckpt[k] == ckpt[k])
                {
                    taken[k] = taken[k] || mtaken[k];
                }
            }
        }

        /// <summary>
        /// Idea for caller: rewrite partitioners to (config, int) pairs instead of connection keys, use that to resolve instance name
        /// </summary>
        /// <param name="targetInstance"></param>
        /// <returns>(clock, ckpt, taken)</returns>
        public void BeforeSend(string targetInstance)
        {
            var k = nameToIndexDict[targetInstance];

            this.sent_to[k] = true;
            this.min_to[k] = Math.Min(this.clock[k], this.clock[i]);
        }

        public (int[], int[], bool[]) GetPiggybackData()
        {
            return (clock, ckpt, taken);
        }

        public void BeforeCheckpoint()
        {
            foreach (int k in nameToIndexDict.Values)
            {
                this.sent_to[k] = false;
                this.min_to[k] = int.MaxValue;
                this.taken[k] = k != i;
            }
        }

        public void AfterCheckpoint()
        {
            this.clock[i]++;
            this.ckpt[i]++;
        }

        public void OnBeforeRestore()
        {
            _logger.Fatal($"BEFORE");
            _logger.Fatal($"clock: {string.Join(", ", clock)}");
            _logger.Fatal($"ckpt: {string.Join(", ", ckpt)}");
            _logger.Fatal($"taken: {string.Join(", ", taken)}");
            _logger.Fatal($"min_to: {string.Join(", ", min_to)}");
            _logger.Fatal($"sent_to: {string.Join(", ", sent_to)}");
        }

        public void OnAfterRestore()
        {
            _logger.Fatal($"AFTER");
            _logger.Fatal($"clock: {string.Join(", ", clock)}");
            _logger.Fatal($"ckpt: {string.Join(", ", ckpt)}");
            _logger.Fatal($"taken: {string.Join(", ", taken)}");
            _logger.Fatal($"min_to: {string.Join(", ", min_to)}");
            _logger.Fatal($"sent_to: {string.Join(", ", sent_to)}");
        }
    }
}
