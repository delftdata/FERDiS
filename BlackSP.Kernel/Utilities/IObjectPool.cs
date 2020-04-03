using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Kernel.Utilities
{
    public interface IObjectPool<T>
    {
        T Rent();

        void Return(T rentee);
    }
}
