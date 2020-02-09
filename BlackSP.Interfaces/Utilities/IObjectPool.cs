using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Interfaces.Utilities
{
    public interface IObjectPool<T>
    {
        T Rent();

        void Return(T rentee);
    }
}
