using BlackSP.Benchmarks.Graph.Events;
using BlackSP.Kernel.Operators;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Benchmarks.Graph.Operators
{
    /// <summary>
    /// Expands a pageupdate event into a set of page events
    /// </summary>
    public class PageUpdateExpansionMapOperator : IMapOperator<PageUpdateEvent, PageEvent>
    {
        public IEnumerable<PageEvent> Map(PageUpdateEvent @event)
        {
            foreach(var page in @event.UpdatedPages)
            {
                yield return new PageEvent
                {
                    Key = page.PageId.ToString(),
                    Page = page
                };
            }
        }
    }
}
