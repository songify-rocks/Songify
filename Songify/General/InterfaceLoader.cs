using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Songify.General
{
    internal static class InterfaceLoader
    {
        public static T Get<T>()
        {
            return GetAll<T>().FirstOrDefault();
        }

        public static List<T> GetAll<T>()
        {
            List<T> interfaces = new();

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
                }
                catch { }
            });

            return interfaces;
        }

        private static List<string> GetDlls()
        {
            return Directory.GetFiles(PathManager.PluginDirectory).Where(x => x.EndsWith(".dll")).ToList();
        }
    }
}
