using BlackSP.Checkpointing.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Persistence
{
    class AzureBackedCheckpointStorage : ICheckpointStorage
    {
        public Checkpoint Retrieve(Guid id)
        {
            //- get memstreampool yay
            //- make blockingcoll with capacity 1-2?
            //- start two threads yay
            //1. sequencially read from stream
            //   pipe to other stream (pool)
            //   
            //2. iterate blocking coll
            //   deserialize to objectsnapshot
            //- take object snapshots and make Checkpoint
            throw new NotImplementedException();
        }

        public void Store(Checkpoint checkpoint)
        {            
            //- get memstreampool yay
            //- make blockingcollection with capacity 1
            //- start two treads yay
            //1. sequencially serialize objectsnapshots (streampool)
            //   add to collection
            //   
            //2. iterate collection 
            //   write to stream one by one
            //- await completion
            //- dispose coll



            throw new NotImplementedException();
        }
    }
}
