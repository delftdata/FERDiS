using BlackSP.Checkpointing.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Checkpointing.Core
{
    [Serializable]
    public class ObjectSnapshot
    {
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

        public void RestoreObject(object target)
        {
            foreach (var fieldinfo in target.GetCheckpointableFields())
            {
                fieldinfo.SetValue(target, _fieldValues[fieldinfo.Name]);
            }
        }
    }
}
