using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Reusability
{
    public class ParameterlessObjectPool<T> : IObjectPool<T> where T : class, new()
    {
        private ConcurrentQueue<T> _objects;

        public ParameterlessObjectPool()
        {
            _objects = new ConcurrentQueue<T>();
        }

        public T Rent()
        {
            if(!_objects.TryDequeue(out T rentee))
            {
                return new T();
            }
            return rentee;
        }

        public void Return(T rentee)
        {
            if(rentee == null) { return; }
            _objects.Enqueue(rentee);
        }
    }
}
