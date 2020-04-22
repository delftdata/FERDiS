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
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) 
                    && p.IsClass 
                    && !p.IsAbstract 
                    && !p.IsInterface
                    && (includePrivate || p.IsPublic));
        }
    }
}
