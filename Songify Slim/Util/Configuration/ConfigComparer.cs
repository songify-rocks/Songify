using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Songify_Slim.Models.Twitch;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Configuration
{
    public static class ConfigComparer
    {
        private static readonly HashSet<string> ExcludedPaths =
        [
            "AppConfig.SongifyApiKey",
            "AppConfig.WebServerPassword",
            "SpotifyCredentials",
            "TwitchCredentials"
        ];

        public static List<string> GetDifferences(object original, object incoming, string prefix = "")
        {
            List<string> diffs = [];
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

        /// <summary>
        /// Detects cloud-import changes that widen who can run Twitch commands or song requests
        /// (lower user levels added). Used to warn in the import preview UI.
        /// </summary>
        public static List<string> GetPermissionWideningWarnings(Configuration local, Configuration incoming)
        {
            List<string> warnings = [];
            if (local == null || incoming == null)
                return warnings;

            CompareUserLevelList(
                warnings,
                "Song request (!sr) command user levels",
                local.AppConfig?.UserLevelsCommand,
                incoming.AppConfig?.UserLevelsCommand);

            CompareUserLevelList(
                warnings,
                "Song request reward user levels",
                local.AppConfig?.UserLevelsReward,
                incoming.AppConfig?.UserLevelsReward);

            List<TwitchCommand> localCmds = local.TwitchCommands?.Commands ?? [];
            List<TwitchCommand> incomingCmds = incoming.TwitchCommands?.Commands ?? [];

            foreach (TwitchCommand incomingCmd in incomingCmds)
            {
                TwitchCommand localCmd = localCmds.FirstOrDefault(c => c.CommandType == incomingCmd.CommandType);
                if (localCmd == null)
                    continue;

                List<int> oldLevels = localCmd.AllowedUserLevels ?? [];
                List<int> newLevels = incomingCmd.AllowedUserLevels ?? [];
                List<int> added = newLevels.Except(oldLevels).ToList();
                if (added.Count == 0)
                    continue;

                bool widened =
                    added.Any(l => l < (int)Enums.TwitchUserLevels.Moderator) ||
                    (oldLevels.Count > 0 && newLevels.Count > 0 && newLevels.Min() < oldLevels.Min());

                if (!widened)
                    continue;

                string trigger = string.IsNullOrWhiteSpace(incomingCmd.Trigger)
                    ? incomingCmd.CommandType.ToString()
                    : "!" + incomingCmd.Trigger;

                warnings.Add(
                    $"{trigger} ({incomingCmd.CommandType}): {FormatLevels(oldLevels)} → {FormatLevels(newLevels)} — more users will be able to run this command.");
            }

            return warnings;
        }

        private static void CompareUserLevelList(
            List<string> warnings,
            string label,
            List<int> oldLevels,
            List<int> newLevels)
        {
            oldLevels ??= [];
            newLevels ??= [];
            List<int> added = newLevels.Except(oldLevels).ToList();
            if (added.Count == 0)
                return;

            bool widened =
                added.Any(l => l < (int)Enums.TwitchUserLevels.Moderator) ||
                (oldLevels.Count > 0 && newLevels.Count > 0 && newLevels.Min() < oldLevels.Min());

            if (!widened)
                return;

            warnings.Add($"{label}: {FormatLevels(oldLevels)} → {FormatLevels(newLevels)} — more users will be allowed.");
        }

        private static string FormatLevels(IEnumerable<int> levels)
        {
            List<int> list = levels?.OrderBy(l => l).ToList() ?? [];
            if (list.Count == 0)
                return "(none)";

            return string.Join(", ", list.Select(l =>
                Enum.IsDefined(typeof(Enums.TwitchUserLevels), l)
                    ? ((Enums.TwitchUserLevels)l).ToString()
                    : l.ToString()));
        }
    }
}
