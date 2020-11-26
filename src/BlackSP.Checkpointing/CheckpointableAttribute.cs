using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class CheckpointableAttribute : Attribute
    {        
        public CheckpointableAttribute()
        {

        }

    }

    public interface ICheckpointableAnnotated
    {
        /// <summary>
        /// Hook that fires right before the checkpointable annotated fields are overwritten
        /// </summary>
        void OnBeforeRestore();

        /// <summary>
        /// Hook that fires right after the checkpointable annotated fields are overwritten
        /// </summary>
        void OnAfterRestore();
    }
}
