using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Core.Operators
{
    class BaseOperator
    {
        //- each vertex pair should have their own endpoints connected
        //--- NO SHARED ENDPOINTS

        //THEREFORE: enqueue outgoing events in all output endpoints,
        //           endpoints will respectively handle partitioning among shards etc
    }
}
