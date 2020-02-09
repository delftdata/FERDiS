using BlackSP.Interfaces.Serialization;
using BlackSP.Interfaces.Utilities;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlackSP.Serialization.Parallelization
{
    /// <summary>
    /// Wrapper for other synchronous serializers that paralellizes serialization
    /// while ensuring ordered reading/writing.
    /// Currently requires parameterless constructors due to objectpool
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ParallelSerializer<T> : ISerializer where T : ISerializer
    {
        private IObjectPool<T> _serializerPool;
        private BlockingCollection<Task> _serializationTasks;

        public ParallelSerializer(IObjectPool<T> serializerPool)
        {
            _serializerPool = serializerPool;

            _serializationTasks = new BlockingCollection<Task>();
            _serializationTasks.Add(Task.CompletedTask);
        }

        public T1 Deserialize<T1>(Stream inputStream, CancellationToken t)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Spawns a new thread which invokes ISerializer.Serialize 
        /// on a serializer from the IObjectpool. Implementation 
        /// ensures writing the bytes in order of invocation while 
        /// parallelizing the serialization process.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="outputStream"></param>
        /// <param name="obj"></param>
        public Task Serialize<T1>(Stream outputStream, T1 obj)
        {
            Task previousSerializationTask = _serializationTasks.Take();
            Task nextSerializationTask = Task.Run(async () =>
            {
                T serializer = default;
                try
                {
                    serializer = _serializerPool.Rent();
                    using (MemoryStream buffer = new MemoryStream())
                    {
                        //serialize object into buffer
                        await serializer.Serialize(buffer, obj);
                        //reset buffer position
                        buffer.Seek(0, SeekOrigin.Begin);
                        //wait untill the previous task is done
                        await previousSerializationTask;
                        //and write result out of buffer
                        buffer.CopyTo(outputStream);
                    }
                }
                finally
                {
                    _serializerPool.Return(serializer);
                }
            });
            _serializationTasks.Add(nextSerializationTask);
            return Task.CompletedTask; //as documented, will spawn background thread so we return instantly
        }
    }
}
