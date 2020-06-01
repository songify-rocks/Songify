using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Songify.Classes;

namespace Songify.Interfaces
{

    public static class InterfaceLoader
    {
        /// <summary>
        /// Get all Assemblys with interface type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> GetAll<T>()
        {
            List<T> interfaces = new List<T>();

            GetDlls().ForEach(d =>
            {
                try
                {
                    foreach (Type t in Assembly.LoadFrom(d).GetTypes())
                    {
                        if (typeof(T) == t) { continue; }
                        if (t.GetInterface(typeof(T).FullName) is null) { continue; }

                            interfaces.Add((T)Activator.CreateInstance(t));
                    }
                } catch { }

            });

            return interfaces;

        }

        /// <summary>
        /// Load all .dll files
        /// </summary>
        /// <returns></returns>
        private static List<string> GetDlls()
        {
            PathManager pm = new PathManager();
            return Directory.GetFiles(pm.PluginDirectory).Where(x => x.EndsWith(".dll")).ToList();
        }
    }
}
