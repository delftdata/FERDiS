using BlackSP.Checkpointing.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Models
{
    [Serializable]
    public class ObjectSnapshot
    {
        public IImmutableDictionary<string, object> FieldValues => _fieldValues.ToImmutableDictionary();

        private readonly IDictionary<string, object> _fieldValues;
        

        public ObjectSnapshot(IDictionary<string, object> fieldValues)
        {
            _fieldValues = fieldValues ?? throw new ArgumentNullException(nameof(fieldValues));
        }

        public static ObjectSnapshot TakeSnapshot(object target)
        {
            var fieldValues = target.GetCheckpointableFieldsAsKeyValuePairs().ToDictionary(x => x.Key, x => x.Value);
            return new ObjectSnapshot(fieldValues);
        }

        /// <summary>
        /// Restores the state stored within this ObjectSnapshot in the provided target
        /// </summary>
        /// <param name="target"></param>
        public void RestoreObject(object target)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            //if hook implemented, call it
            (target as ICheckpointableAnnotated)?.OnBeforeRestore();
            //do restore
            foreach (var fieldinfo in target.GetCheckpointableFields())
            {
                fieldinfo.SetValue(target, _fieldValues[fieldinfo.Name]);
                var cpAttr = fieldinfo.GetCustomAttributes(true)
                    .Select(attr => attr as CheckpointableAttribute)
                    .Where(val => val != null)
                    .First();
            }
            //if hook implemented, call it
            (target as ICheckpointableAnnotated)?.OnAfterRestore();
        }

        protected bool Equals(ObjectSnapshot other)
        {
            var shallowEqual = other != null 
                && _fieldValues.Keys.OrderBy(k => k).SequenceEqual(other.FieldValues.Keys.OrderBy(k => k));
            if(!shallowEqual)
            {
                return false;
            }
            var deepEqual = true;
            foreach(var key in _fieldValues.Keys)
            {
                deepEqual = deepEqual && _fieldValues[key]?.GetType() == other.FieldValues[key]?.GetType();
            }
            return deepEqual;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ObjectSnapshot);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
