using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.Core.Extensions
{
    internal static class TCSExtensions
    {

        /// <summary>
        /// Reinstantiates the tcs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static void Reset<T>(this TaskCompletionSource<T> source, TaskCreationOptions options = TaskCreationOptions.RunContinuationsAsynchronously) {
            source = new TaskCompletionSource<T>(options);
        }

    }
}
