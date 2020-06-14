using BlackSP.Core.OperatorShells;
using BlackSP.Kernel.Models;
using BlackSP.Kernel.Operators;
using System;

namespace BlackSP.Infrastructure.Configuration.Operators
{

    public class JoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut> : ProducingOperatorConfiguratorBase<TOut>, IJoinOperatorConfigurator<TOperator, TIn1, TIn2, TOut>
        where TOperator : IJoinOperator<TIn1, TIn2, TOut>
        where TIn1 : class, IEvent
        where TIn2 : class, IEvent
        where TOut : class, IEvent
    {

        public override Type OperatorType => typeof(JoinOperatorShell<TIn1, TIn2, TOut>);
        public override Type OperatorConfigurationType => typeof(TOperator);

        public JoinOperatorConfigurator(string[] instanceNames, string operatorName) : base(instanceNames, operatorName)
        { }
    }
}
