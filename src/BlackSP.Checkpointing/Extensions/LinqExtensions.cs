using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Extensions
{
    public static class LinqExtensions
    {

        public static IEnumerable<Stack<T>> WhereStackNonEmpty<T>(this IEnumerable<Stack<T>> stacks)
        {
            return stacks.Where(stack => stack.Count > 0);
        }

    }
}
