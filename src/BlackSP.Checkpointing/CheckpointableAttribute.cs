using System;
using System.Collections.Generic;
using System.Text;

namespace BlackSP.Checkpointing
{

    /// <summary>
    /// Annotation for class fields to mark them as part of application state and that the field values should be part of checkpoints and be restored on checkpoint restore.<br/>
    /// Combine with the <see cref="ICheckpointableAnnotated "/> interface to implement checkpoint related hooks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class CheckpointableAttribute : Attribute
    { }

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
