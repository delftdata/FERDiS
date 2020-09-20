using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlackSP.Serialization.Utilities
{
    public static class TypeLoader
    {
        public static IEnumerable<Type> GetClassesExtending(Type interfaceType, bool includePrivate = true)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies
                .Where(a =>
                {
                    //nasty fix for a weird version incompatibility issue with azure.core's system.diagnostics.diagnosticsource dependency
                    //so far only showed up here
                    bool ok = !a.FullName.StartsWith("System.") && !a.FullName.StartsWith("Azure.") && !a.FullName.StartsWith("Microsoft.");
                    //other nasty fix for a weird set of types with GUID for names that cannot always get loaded
                    ok = ok && !Guid.TryParse(a.FullName.Split(',')[0], out _);
                    return ok;
                })
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) 
                    && p.IsClass 
                    && !p.IsAbstract 
                    && !p.IsInterface
                    && (includePrivate || p.IsPublic))
                .ToList();
        }
    }
}
