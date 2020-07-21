using BlackSP.Checkpointing.Extensions;
using BlackSP.Checkpointing.Core;
using BlackSP.Kernel.Checkpointing;
using BlackSP.Serialization.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BlackSP.Checkpointing
{
    /// <summary>
    /// 
    /// </summary>
    public class CheckpointService : ICheckpointService
    {

        private readonly ObjectRegistry _register;

        public CheckpointService(ObjectRegistry register)
        {
            _register = register ?? throw new ArgumentNullException(nameof(register));
        }

        /// <summary>
        /// Register an [ApplicationState] annotated class instance, will track registered object and include it in checkpoint creation and restoration<br/>
        /// Will ignore registration invocations with non-annotated class instances;
        /// </summary>
        /// <param name="o"></param>
        public bool Register(object o)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));

            var type = o.GetType();
            var identifier = type.AssemblyQualifiedName; //Note: currently the implementation supports only one instance of any concrete type 
            //Under the assumption of exact same object registration order (at least regarding instances of the same type) this could be extended more or less easily

            if(!o.AssertCheckpointability())
            {
                Console.WriteLine($"Type {type} is not checkpointable.");
                return false;
            }
            Console.WriteLine($"Object of type {type} is registered for checkpointing.");
            _register.Add(identifier, o);
            return true;
        }

        /// <summary>
        /// Take a checkpoint, returns registered object's [ApplicationState] annotated property values serialized in a restorable format
        /// </summary>
        /// <returns></returns>
        public byte[] Checkpoint()
        {
            var checkpoint = _register.TakeCheckpoint();
            return checkpoint.BinarySerialize();
        }

        /// <summary>
        /// Restore a checkpoint, only returns false when there is a discrepancy between the objects registered and the objects in the checkpoint
        /// </summary>
        /// <param name="checkpointBytes"></param>
        /// <returns></returns>
        public void Restore(byte[] checkpointBytes)
        {
            _ = checkpointBytes ?? throw new ArgumentNullException(nameof(checkpointBytes));

            var o = checkpointBytes.BinaryDeserialize();

            var checkpoint = o as Checkpoint;
            if (checkpoint == null)
            {
                throw new ArgumentException($"Attempted to restore state from object of invalid type: {o.GetType()}, expected: {nameof(IDictionary<string, object>)}", nameof(o));
            }

            _register.RestoreCheckpoint(checkpoint);
        }

    }
}
