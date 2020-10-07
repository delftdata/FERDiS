﻿using BlackSP.Checkpointing.Attributes;
using BlackSP.Kernel.Operators;
using BlackSP.WordCount.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.WordCount.Operators
{
    class WordCountLoggerSink : ISinkOperator<WordEvent>
    {

        private readonly ILogger _logger;

        [Checkpointable]
        private IDictionary<string, int> _wordCountMap;
        
        public WordCountLoggerSink(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _wordCountMap = new Dictionary<string, int>();
        }

        public Task Sink(WordEvent @event)
        {
            if(_wordCountMap.ContainsKey(@event.Word))
            {
                _wordCountMap[@event.Word] += @event.Count;
            } 
            else
            {
                _wordCountMap.Add(@event.Word, @event.Count);
            }
            string wordCountMapPrintable = string.Join(";", _wordCountMap.Select(x => x.Key + "=" + x.Value).ToArray());

            _logger.Information($"WordCount: {wordCountMapPrintable}");
            return Task.CompletedTask;
        }
    }
}