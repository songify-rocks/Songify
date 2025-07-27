using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Settings
{
    public static class ConfigComparer
    {
        static readonly HashSet<string> ExcludedPaths = new HashSet<string>
        {
            "AppConfig.SongifyApiKey",
            "SpotifyCredentials",
            "TwitchCredentials",
        };


        public static List<string> GetDifferences(object original, object incoming, string prefix = "")
        {
            List<string> diffs = new();
            if (original == null || incoming == null)
                return diffs;

            Type type = original.GetType();
            if (type != incoming.GetType())
                return diffs;

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Skip problematic properties
                if (prop.GetIndexParameters().Length > 0) // 💡 this skips indexers like List<T>.Item
                    continue;

                if (prop.Name == "SyncRoot" || prop.Name == "IsReadOnly")
                    continue;

                object originalValue = prop.GetValue(original);
                object incomingValue = prop.GetValue(incoming);
                string fullName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                if (ExcludedPaths.Any(p => fullName.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                                           fullName.StartsWith(p + ".", StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (originalValue == null && incomingValue == null)
                    continue;

                // Recurse into custom objects
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string)
                    && !typeof(IEnumerable<object>).IsAssignableFrom(prop.PropertyType))
                {
                    if (originalValue != null && incomingValue != null)
                    {
                        diffs.AddRange(GetDifferences(originalValue, incomingValue, fullName));
                    }
                    else
                    {
                        diffs.Add($"{fullName}: {(originalValue ?? "null")} → {(incomingValue ?? "null")}");
                    }
                }
                // Compare collections as-is (or handle more deeply if needed)
                else if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                         && prop.PropertyType != typeof(string))
                {
                    IEnumerable newEnum = incomingValue as IEnumerable;

                    string oldList = originalValue is IEnumerable oldEnum ? string.Join(", ", oldEnum.Cast<object>()) : "null";
                    string newList = newEnum != null ? string.Join(", ", newEnum.Cast<object>()) : "null";

                    if (oldList != newList)
                        diffs.Add($"{fullName}: {oldList} → {newList}");
                }

                else
                {
                    if (!Equals(originalValue, incomingValue))
                    {
                        string oVal = originalValue?.ToString() ?? "null";
                        string iVal = incomingValue?.ToString() ?? "null";
                        diffs.Add($"{fullName}: {oVal} → {iVal}");
                    }
                }
            }

            return diffs;
        }

    }

}
