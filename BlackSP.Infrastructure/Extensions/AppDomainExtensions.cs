using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BlackSP.Infrastructure.Extensions
{
    public static class AppDomainExtensions
    {

        public static void LoadAllAvailableAssemblies(this AppDomain appDomain, Assembly targetExecutingAssembly)
        {
            _ = appDomain ?? throw new ArgumentNullException(nameof(appDomain));

            var loadedAssemblies = appDomain.GetAssemblies().ToList();
            var loadedPaths = loadedAssemblies.Where(a => !a.IsDynamic).Select(a => a.Location).ToArray();

            string assemblyDir = Path.GetDirectoryName(targetExecutingAssembly.Location);
            Console.WriteLine($"I think assembly dir is: {assemblyDir}, from executing assembly: {targetExecutingAssembly.FullName}");
            var referencedPaths = Directory.GetFiles(assemblyDir, "*.dll");
            var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
            
            foreach(var path in toLoad)
            {
                try
                {
                    loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
                } 
                catch(Exception e)
                {
                    Console.WriteLine("Failed to load assembly at location: " + path);
                }
            }
        }

        
    }
}
