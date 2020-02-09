using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Reusability
{
    public interface IObjectPool<T> where T : class, new()
    {
        T Rent();

        void Return(T rentee);
    }
}
