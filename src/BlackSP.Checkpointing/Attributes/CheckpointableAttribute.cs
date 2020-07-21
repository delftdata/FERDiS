using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class CheckpointableAttribute : Attribute
    {

        
        public CheckpointableAttribute()
        {
        }

        
    }
}
