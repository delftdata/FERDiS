using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.CRA.Configuration
{
    /// <summary>
    /// Model class that holds data regarding an edge in an operator graph
    /// </summary>
    public class Edge
    {
        public IOperatorConfigurator FromOperator { get; private set; }
        public string FromEndpoint { get; private set; }

        public IOperatorConfigurator ToOperator { get; private set; }
        public string ToEndpoint { get; private set; }

        public Edge(IOperatorConfigurator fromOperator, string fromEndpoint, IOperatorConfigurator toOperator, string toEndpoint)
        {
            FromOperator = fromOperator ?? throw new ArgumentNullException(nameof(fromOperator));
            FromEndpoint = fromEndpoint ?? throw new ArgumentNullException(nameof(fromEndpoint));

            ToOperator = toOperator ?? throw new ArgumentNullException(nameof(toOperator));
            ToEndpoint = toEndpoint ?? throw new ArgumentNullException(nameof(toEndpoint));

        }
    }
}
