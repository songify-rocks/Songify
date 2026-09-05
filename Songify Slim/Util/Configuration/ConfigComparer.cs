using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Songify_Slim.Models.Twitch;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Configuration
{
    public sealed class ConfigDiffItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public string Path { get; init; }
        public string Group { get; init; }
        public string DisplayName { get; init; }
        public string OldText { get; init; }
        public string NewText { get; init; }
        public bool IsSecret { get; init; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class ConfigDiffGroup
    {
        public string Title { get; init; }
        public List<ConfigDiffItem> Items { get; init; } = [];
    }

    public static class ConfigComparer
    {
        private static readonly HashSet<string> ExcludedPaths =
        [
            "AppConfig.SongifyApiKey",
            "AppConfig.YoutubeApiKey",
            "AppConfig.WebServerPassword",
            "AppConfig.Uuid",
            "AppConfig.UpdateRequired",
            "AppConfig.LastShownMotdId",
            "AppConfig.ReadNotificationIds",
            "AppConfig.SpotifyPersistentIssue",
            "AppConfig.SpotifyPersistentIssues",
            // Legacy — artists live in BlockedSpotifyArtists; AppConfig field is always empty at runtime.
            "AppConfig.ArtistBlacklist",
            "SpotifyCredentials",
            "TwitchCredentials"
        ];

        private static readonly HashSet<string> LocalCredentialRoots =
        [
            "SpotifyCredentials",
            "TwitchCredentials"
        ];

        private static readonly HashSet<string> SecretLeafPaths =
        [
            "AppConfig.SongifyApiKey",
            "AppConfig.YoutubeApiKey",
            "AppConfig.WebServerPassword",
            "SpotifyCredentials.AccessToken",
            "SpotifyCredentials.RefreshToken",
            "SpotifyCredentials.ClientSecret",
            "TwitchCredentials.AccessToken",
            "TwitchCredentials.BotOAuthToken",
            "TwitchCredentials.TwitchBotToken"
        ];

        private const int MaxSimpleListItems = 24;

        public static List<ConfigDiffItem> GetDiffItems(Configuration local, Configuration incoming, bool includeCredentials)
        {
            List<ConfigDiffItem> items = [];
            CollectItems(local, incoming, "", includeCredentials, skipMissingIncomingSection: true, items);
            return items;
        }

        public static List<ConfigDiffGroup> GroupDiffs(IEnumerable<ConfigDiffItem> items)
        {
            return items
                .GroupBy(i => i.Group)
                .Select(g => new ConfigDiffGroup { Title = g.Key, Items = [.. g] })
                .ToList();
        }

        public static List<string> GetDifferences(object original, object incoming, string prefix = "")
        {
            List<ConfigDiffItem> items = [];
            CollectItems(original, incoming, prefix, includeCredentials: false, skipMissingIncomingSection: false, items);
            return items.Select(i => $"{i.Path}: {i.OldText} → {i.NewText}").ToList();
        }

        public static void CopySelected(Configuration target, Configuration source, IEnumerable<string> paths)
        {
            if (target == null || source == null || paths == null)
                return;

            foreach (string path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                CopyPath(target, source, path.Trim());
            }
        }

        private static void CollectItems(
            object original,
            object incoming,
            string prefix,
            bool includeCredentials,
            bool skipMissingIncomingSection,
            List<ConfigDiffItem> items)
        {
            if (original == null || incoming == null)
                return;

            Type type = original.GetType();
            if (type != incoming.GetType())
                return;

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                if (prop.Name is "SyncRoot" or "IsReadOnly" or "Count" or "Capacity")
                    continue;

                object originalValue = prop.GetValue(original);
                object incomingValue = prop.GetValue(incoming);
                string fullName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                if (IsExcluded(fullName, includeCredentials))
                    continue;

                if (skipMissingIncomingSection &&
                    string.IsNullOrEmpty(prefix) &&
                    incomingValue == null &&
                    prop.PropertyType.IsClass &&
                    prop.PropertyType != typeof(string))
                    continue;

                if (originalValue == null && incomingValue == null)
                    continue;

                bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                                    && prop.PropertyType != typeof(string);

                if (IsTwitchCommandList(prop, originalValue, incomingValue))
                {
                    CollectCommandDiffs(originalValue as IEnumerable, incomingValue as IEnumerable, items);
                    continue;
                }

                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !isEnumerable)
                {
                    if (originalValue != null && incomingValue != null)
                    {
                        CollectItems(originalValue, incomingValue, fullName, includeCredentials, false, items);
                    }
                    else
                    {
                        AddItem(items, fullName, FormatScalar(originalValue), FormatScalar(incomingValue));
                    }
                }
                else if (isEnumerable)
                {
                    if (!CollectionsEqual(originalValue as IEnumerable, incomingValue as IEnumerable, out string oldText, out string newText))
                        AddItem(items, fullName, oldText, newText);
                }
                else if (!Equals(originalValue, incomingValue))
                {
                    AddItem(items, fullName, FormatScalar(originalValue), FormatScalar(incomingValue));
                }
            }
        }

        private static bool IsExcluded(string fullName, bool includeCredentials)
        {
            foreach (string p in ExcludedPaths)
            {
                bool match = fullName.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                             fullName.StartsWith(p + ".", StringComparison.OrdinalIgnoreCase);
                if (!match)
                    continue;

                if (includeCredentials && LocalCredentialRoots.Any(r =>
                        p.Equals(r, StringComparison.OrdinalIgnoreCase)))
                    continue;

                return true;
            }

            return false;
        }

        private static bool IsTwitchCommandList(PropertyInfo prop, object originalValue, object incomingValue)
            => prop.Name == "Commands"
               && (originalValue is IEnumerable<TwitchCommand> || incomingValue is IEnumerable<TwitchCommand>);

        private static void CollectCommandDiffs(IEnumerable original, IEnumerable incoming, List<ConfigDiffItem> items)
        {
            List<TwitchCommand> oldCmds = (original as IEnumerable)?.Cast<object>().OfType<TwitchCommand>().ToList() ?? [];
            List<TwitchCommand> newCmds = (incoming as IEnumerable)?.Cast<object>().OfType<TwitchCommand>().ToList() ?? [];

            foreach (TwitchCommand incomingCmd in newCmds)
            {
                TwitchCommand localCmd = oldCmds.FirstOrDefault(c => c.CommandType == incomingCmd.CommandType);
                string path = $"TwitchCommands.Commands[{incomingCmd.CommandType}]";
                if (localCmd == null)
                {
                    AddItem(items, path, "(missing)", SummarizeCommand(incomingCmd));
                    continue;
                }

                if (CommandEquals(localCmd, incomingCmd))
                    continue;

                AddItem(items, path, SummarizeCommand(localCmd), SummarizeCommand(incomingCmd));
            }
        }

        private static bool CommandEquals(TwitchCommand a, TwitchCommand b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;
            return string.Equals(ConfigHandler.CloneToYaml(a), ConfigHandler.CloneToYaml(b), StringComparison.Ordinal);
        }

        private static string SummarizeCommand(TwitchCommand cmd)
        {
            if (cmd == null)
                return "null";
            string trigger = string.IsNullOrWhiteSpace(cmd.Trigger) ? cmd.CommandType.ToString() : "!" + cmd.Trigger;
            string on = cmd.IsEnabled ? "on" : "off";
            return $"{trigger} ({on})";
        }

        private static void AddItem(List<ConfigDiffItem> items, string path, string oldText, string newText)
        {
            bool secret = SecretLeafPaths.Any(s => path.Equals(s, StringComparison.OrdinalIgnoreCase));
            items.Add(new ConfigDiffItem
            {
                Path = path,
                Group = GroupForPath(path),
                DisplayName = DisplayNameForPath(path),
                OldText = oldText,
                NewText = newText,
                IsSecret = secret,
                IsSelected = !secret
            });
        }

        private static string GroupForPath(string path)
        {
            if (path.StartsWith("BotConfig", StringComparison.OrdinalIgnoreCase))
                return Loc("window_import_group_bot", "Bot responses");
            if (path.StartsWith("TwitchCommands", StringComparison.OrdinalIgnoreCase))
                return Loc("window_import_group_commands", "Twitch commands");
            if (path.StartsWith("BlockedSpotifyArtists", StringComparison.OrdinalIgnoreCase))
                return Loc("window_import_group_artists", "Blocked artists");
            if (path.StartsWith("SpotifyCredentials", StringComparison.OrdinalIgnoreCase))
                return Loc("window_import_group_spotify", "Spotify account");
            if (path.StartsWith("TwitchCredentials", StringComparison.OrdinalIgnoreCase))
                return Loc("window_import_group_twitch", "Twitch account");
            return Loc("window_import_group_app", "App settings");
        }

        private static string DisplayNameForPath(string path)
        {
            if (path.StartsWith("TwitchCommands.Commands[", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(']'))
            {
                string type = path[(path.IndexOf('[') + 1)..^1];
                return type;
            }

            string leaf = path;
            int dot = path.LastIndexOf('.');
            if (dot >= 0 && dot < path.Length - 1)
                leaf = path[(dot + 1)..];

            return Regex.Replace(leaf, "([a-z])([A-Z])", "$1 $2");
        }

        private static string Loc(string key, string fallback)
            => System.Windows.Application.Current?.TryFindResource(key) as string ?? fallback;

        private static void CopyPath(Configuration target, Configuration source, string path)
        {
            if (path.StartsWith("TwitchCommands.Commands[", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(']') &&
                Enum.TryParse(path[(path.IndexOf('[') + 1)..^1], out Enums.CommandType cmdType))
            {
                target.TwitchCommands ??= new TwitchCommands { Commands = [] };
                target.TwitchCommands.Commands ??= [];
                source.TwitchCommands ??= new TwitchCommands { Commands = [] };

                TwitchCommand incomingCmd = source.TwitchCommands.Commands?
                    .FirstOrDefault(c => c.CommandType == cmdType);
                if (incomingCmd == null)
                    return;

                TwitchCommand clone = ConfigHandler.CloneYaml(incomingCmd);
                int index = target.TwitchCommands.Commands.FindIndex(c => c.CommandType == cmdType);
                if (index >= 0)
                    target.TwitchCommands.Commands[index] = clone;
                else
                    target.TwitchCommands.Commands.Add(clone);
                return;
            }

            object t = target;
            object s = source;
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                PropertyInfo prop = t?.GetType().GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo srcProp = s?.GetType().GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || srcProp == null)
                    return;

                if (i == parts.Length - 1)
                {
                    object value = srcProp.GetValue(s);
                    if (value != null && prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                        value = ConfigHandler.CloneYamlObject(value);
                    if (prop.CanWrite)
                        prop.SetValue(t, value);
                    return;
                }

                object nextT = prop.GetValue(t);
                object nextS = srcProp.GetValue(s);
                if (nextS == null)
                    return;

                if (nextT == null)
                {
                    nextT = Activator.CreateInstance(prop.PropertyType);
                    prop.SetValue(t, nextT);
                }

                t = nextT;
                s = nextS;
            }
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

            CompareUserLevelList(
                warnings,
                "Explicit song request user levels",
                local.AppConfig?.UserLevelsExplicitSongs,
                incoming.AppConfig?.UserLevelsExplicitSongs);

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
