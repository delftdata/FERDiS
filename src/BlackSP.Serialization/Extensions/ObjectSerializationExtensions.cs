using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace BlackSP.Serialization.Extensions
{
    /// <summary>
    /// Extensions for simple object binary formatting
    /// </summary>
    public static class ObjectSerializationExtensions
    {
        public static byte[] BinarySerialize(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        public static object BinaryDeserialize(this byte[] arrBytes)
        {
            using (var memoryStream = new MemoryStream(arrBytes))
            {
                var binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}
