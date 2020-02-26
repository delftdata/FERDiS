using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using BlackSP.Interfaces.Utilities;

namespace BlackSP.Core.Reusability
{
    /// <summary>
    /// Provides a Rent/Return interface for renting class instances
    /// of any type that implements a parameterless constructor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
            rentee = rentee ?? throw new ArgumentNullException(nameof(rentee));
            _objects.Enqueue(rentee);
        }
    }
}
