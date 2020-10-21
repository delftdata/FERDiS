using BlackSP.Checkpointing.Attributes;
using BlackSP.Checkpointing.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace BlackSP.Checkpointing.Extensions
{
    static class CheckpointabilityExtensions
    {

        /// <summary>
        /// Checks the type of the provided object for presence of Checkpointable attribute on fields and serializability of the field types<br/>
        /// Returns false if no checkpointable attributes were found<br/>
        /// Returns true if checkpointable fields were found and the types are serializable<br/>
        /// Throws CheckpointPreconditionException when checkpointable fields were found and the types are not serializable
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool AssertCheckpointability(this object o)
        {
            var checkpointableFields = o.GetCheckpointableFields();
            if(!checkpointableFields.Any())
            {
                return false;
            }
            //checks the serializability of the field's value type, the field type itself is a bad indicator (ICollection vs List)
            //if the value is a null we cant say it is serializable so default to false
            if (checkpointableFields.All(f => f.GetValue(o)?.GetType().IsSerializable ?? false))
            {
                return true;
            }

            throw new CheckpointingPreconditionException($"Object of type {o.GetType()} implements one or multiple checkpointable fields which are not of serializable type");
        }

        public static IEnumerable<FieldInfo> GetCheckpointableFields(this object o)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));

            List<FieldInfo> fields = new List<FieldInfo>();

            var baseType = o.GetType();
            while (baseType != null)
            {
                fields.AddRange(baseType.GetRuntimeFields().Where(field => field.HasCheckpointableAttribute()));
                baseType = baseType.BaseType;
            }
            return fields;
        }

        public static bool HasCheckpointableAttribute(this FieldInfo field)
        {
            _ = field ?? throw new ArgumentNullException(nameof(field));
            return field.CustomAttributes.Any(attrData => attrData.AttributeType == typeof(CheckpointableAttribute));
        }

        public static IEnumerable<KeyValuePair<string, object>> GetCheckpointableFieldsAsKeyValuePairs(this object o)
        {
            foreach (var fieldinfo in o.GetCheckpointableFields()) {
                yield return new KeyValuePair<string, object>(fieldinfo.Name, fieldinfo.GetValue(o));
            }
        }

    }
}
