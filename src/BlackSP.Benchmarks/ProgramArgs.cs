using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks
{
    public enum Infrastructure
    {
        Simulator,
        CRA
    }

    public enum Job
    {
        WordCount,
        Selection,
        LocalItem,
        HotItem,
        AverageSellingPriceBySeller,
        NHop
    }

    public enum Size
    {
        Small,
        Medium,
        Large
    }
}
