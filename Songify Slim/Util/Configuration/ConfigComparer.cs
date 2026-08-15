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
            // Legacy — artists live in BlockedSpotifyArtists; AppConfig field is always empty at runtime.
            "AppConfig.ArtistBlacklist",
            "SpotifyCredentials",
            "TwitchCredentials"
        ];

        private const int MaxSimpleListItems = 24;

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

                if (prop.Name is "SyncRoot" or "IsReadOnly" or "Count" or "Capacity")
                    continue;

                object originalValue = prop.GetValue(original);
                object incomingValue = prop.GetValue(incoming);
                string fullName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                if (ExcludedPaths.Any(p => fullName.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                                           fullName.StartsWith(p + ".", StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (originalValue == null && incomingValue == null)
                    continue;

                bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                                    && prop.PropertyType != typeof(string);

                // Recurse into custom objects (not collections)
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !isEnumerable)
                {
                    if (originalValue != null && incomingValue != null)
                    {
                        diffs.AddRange(GetDifferences(originalValue, incomingValue, fullName));
                    }
                    else
                    {
                        diffs.Add($"{fullName}: {FormatScalar(originalValue)} → {FormatScalar(incomingValue)}");
                    }
                }
                else if (isEnumerable)
                {
                    if (!CollectionsEqual(originalValue as IEnumerable, incomingValue as IEnumerable, out string oldText, out string newText))
                        diffs.Add($"{fullName}: {oldText} → {newText}");
                }
                else
                {
                    if (!Equals(originalValue, incomingValue))
                        diffs.Add($"{fullName}: {FormatScalar(originalValue)} → {FormatScalar(incomingValue)}");
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

        private static bool CollectionsEqual(
            IEnumerable original,
            IEnumerable incoming,
            out string oldText,
            out string newText)
        {
            List<object> oldItems = ToList(original);
            List<object> newItems = ToList(incoming);

            oldText = FormatCollection(oldItems);
            newText = FormatCollection(newItems);

            if (oldItems.Count != newItems.Count)
                return false;

            if (oldItems.Count == 0)
                return true;

            // Primitives / strings: compare formatted content (already capped for display).
            if (IsSimpleElement(oldItems[0]))
                return string.Equals(FingerprintSimple(oldItems), FingerprintSimple(newItems), StringComparison.Ordinal);

            // Complex objects: compare stable identity fingerprints (Key/Id/Name/…), not type.ToString().
            return string.Equals(FingerprintComplex(oldItems), FingerprintComplex(newItems), StringComparison.Ordinal);
        }

        private static List<object> ToList(IEnumerable value)
        {
            if (value == null)
                return [];
            return value.Cast<object>().Where(x => x != null).ToList();
        }

        private static string FormatCollection(List<object> items)
        {
            if (items == null || items.Count == 0)
                return "0 items";

            if (IsSimpleElement(items[0]))
            {
                IEnumerable<string> shown = items.Take(MaxSimpleListItems).Select(FormatScalar);
                string joined = string.Join(", ", shown);
                if (items.Count > MaxSimpleListItems)
                    joined += $", … (+{items.Count - MaxSimpleListItems} more)";
                return joined;
            }

            return items.Count == 1 ? "1 item" : $"{items.Count} items";
        }

        private static bool IsSimpleElement(object item)
        {
            if (item == null)
                return true;
            Type t = item.GetType();
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t.IsPrimitive
                   || t.IsEnum
                   || t == typeof(string)
                   || t == typeof(decimal)
                   || t == typeof(DateTime)
                   || t == typeof(DateTimeOffset)
                   || t == typeof(Guid)
                   || t == typeof(TimeSpan);
        }

        private static string FormatScalar(object value)
        {
            if (value == null)
                return "null";
            if (value is string s)
                return string.IsNullOrEmpty(s) ? "(empty)" : s;
            return value.ToString() ?? "null";
        }

        private static string FingerprintSimple(List<object> items)
            => string.Join("\n", items.Select(FormatScalar));

        private static string FingerprintComplex(List<object> items)
        {
            // Sorted so order differences alone don't create noisy diffs for set-like lists (blocklists).
            return string.Join("\n", items.Select(GetObjectIdentity).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private static string GetObjectIdentity(object item)
        {
            if (item == null)
                return "";

            Type t = item.GetType();
            foreach (string name in new[] { "Key", "Id", "TrackId", "SongId", "ArtistId", "UserId", "CommandType", "Name", "Title", "Display" })
            {
                PropertyInfo prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || prop.GetIndexParameters().Length > 0)
                    continue;
                object val = prop.GetValue(item);
                if (val == null)
                    continue;
                string text = val.ToString();
                if (!string.IsNullOrWhiteSpace(text) && text != t.FullName)
                    return $"{t.Name}:{name}={text}";
            }

            // Last resort: avoid default Object.ToString() type dump in equality checks.
            return $"{t.Name}#{item.GetHashCode()}";
        }
    }
}
