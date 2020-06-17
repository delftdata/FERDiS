using BlackSP.Kernel.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Models.Payloads
{
    [ProtoContract]
    public class WorkerRequestPayload : MessagePayloadBase
    {
        public static new string MetaDataKey => "control:workerrequest";
        public RequestType RequestType { get; set; } 
    }

    public enum RequestType
    {
        /// <summary>
        /// Requests a status report
        /// </summary>
        Status,
        /// <summary>
        /// Requests the worker to start processing data
        /// </summary>
        StartProcessing,
        /// <summary>
        /// Requests the worker to stop processing data<br/>
        /// <b>Note: implicitly requests flushing the in- and output channels</b>
        /// </summary>
        StopProcessing,
        
    }
}
