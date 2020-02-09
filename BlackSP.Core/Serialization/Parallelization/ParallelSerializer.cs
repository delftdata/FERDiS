using BlackSP.Core.Events;
using BlackSP.Core.Reusability;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Serialization.Parallelization
{
    public class ParallelSerializer<T> : IParallelEventSerializer where T : class, IEventSerializer, new()
    {
        private IObjectPool<T> _serializerPool;
        private BlockingCollection<Task> _serializationTasks;


        public ParallelSerializer(IObjectPool<T> serializerPool)
        {
            _serializerPool = serializerPool;

            _serializationTasks = new BlockingCollection<Task>();
            _serializationTasks.Add(Task.CompletedTask);
        }

        public void StartSerialization(Stream outputStream, IEvent @event)
        {
            Task previousSerializationTask = _serializationTasks.Take();
            Task nextSerializationTask = Task.Run(async () =>
            {
                T serializer = null;
                try
                {
                    serializer = _serializerPool.Rent();
                    using (MemoryStream buffer = new MemoryStream())
                    {
                        //serialize event into buffer
                        serializer.SerializeEvent(buffer, @event);
                        //reset buffer position
                        buffer.Seek(0, SeekOrigin.Begin);
                        //wait untill the previous task is done
                        await previousSerializationTask;
                        //and write result out of buffer
                        buffer.CopyTo(outputStream);
                    }
                } finally
                {
                    _serializerPool.Return(serializer);
                }
            });
            _serializationTasks.Add(nextSerializationTask);
        }
    }
}
