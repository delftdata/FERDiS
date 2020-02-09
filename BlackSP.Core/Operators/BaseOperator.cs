using System;
using System.Collections.Generic;
using System.Text;
using BlackSP.Interfaces.Operators;

namespace BlackSP.Core.Operators
{
    public class BaseOperator : IOperator
    {
        //- each vertex pair should have their own endpoints connected
        //--- NO SHARED ENDPOINTS

        //THEREFORE: enqueue outgoing events in all output endpoints,
        //           endpoints will respectively handle partitioning among shards etc
    }
}
