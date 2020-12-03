using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BlackSP.Serialization.Utilities
{
    public static class TypeLoader
    {
        public static IEnumerable<Type> GetClassesExtending(Type interfaceType, bool includePrivate = true)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies
                .SafeGetTypes()
                .Where(p => interfaceType.IsAssignableFrom(p) 
                    && p.IsClass 
                    && !p.IsAbstract 
                    && !p.IsInterface
                    && (includePrivate || p.IsPublic))
                .ToList();
        }

        public static IEnumerable<Type> SafeGetTypes(this Assembly[] assemblies)
        {
            return assemblies.Where(a =>
            {
                //nasty fix for a weird version incompatibility issue with azure.core's system.diagnostics.diagnosticsource dependency
                //so far only showed up here
                bool ok = !a.FullName.StartsWith("System.") && !a.FullName.StartsWith("Azure.") && !a.FullName.StartsWith("Microsoft.");
                //other nasty fix for a weird set of types with GUID for names that cannot always get loaded
                ok = ok && !Guid.TryParse(a.FullName.Split(',')[0], out _);
                return ok;
            })
            .SelectMany(s => s.GetTypes());
        }

        /// <summary>
        /// Gets all loaded types that have protobuf annotations
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetProtobufAnnotatedTypes()
        {
            var contractAttributeType = typeof(ProtoContractAttribute);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies
                .SafeGetTypes()
                .Where(t => t.CustomAttributes.Any(cad => cad.AttributeType == contractAttributeType))
                .Where(t => t.IsClass);
        }

        public static IEnumerable<Type> GetAllBaseTypes(this Type t)
        {
            var baseType = t.BaseType;
            while(baseType != null)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        /// <summary>
        /// Retrieves the highest base type found under System.Object
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Type GetHighestBaseType(this Type t)
        {
            var baseType = t;
            while (baseType != null)
            {
                if(baseType.BaseType == typeof(object))
                {
                    return baseType;
                }
                baseType = baseType.BaseType;
            }
            throw new ArgumentException($"Type {t} does not have {typeof(object)} in its hierarchy");
        }
    }
}
