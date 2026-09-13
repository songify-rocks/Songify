using System;
using System.Collections.Generic;
using System.Linq;

namespace Songify_Slim.Models.Responses
{
    public class Psa
    {
        public int Id { get; set; }
        public string MessageText { get; set; }
        public string Severity { get; set; }
        public long? CreatedAt { get; set; }  // Unix timestamp in seconds
        public long? StartDate { get; set; }  // Unix timestamp in seconds
        public long? EndDate { get; set; }    // Unix timestamp in seconds
        public bool IsActive { get; set; }
        public string Author { get; set; }
        /// <summary>`All` (default), `Stable`, `Beta`, `Dev`, or a comma-separated list.</summary>
        public string Channel { get; set; }
        /// <summary>Inclusive lower bound (e.g. `1.8.0`). Null = no lower bound.</summary>
        public string MinVersion { get; set; }
        /// <summary>Inclusive upper bound. Null = no upper bound. Set equal to MinVersion for an exact version.</summary>
        public string MaxVersion { get; set; }

        /// <summary>Info=0 … Critical=4. Unknown values count as Info.</summary>
        public int SeverityRank => (Severity ?? "").Trim().ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };

        public bool IsUrgent => SeverityRank >= 3;

        /// <summary>
        /// Whether this message should be shown to a client.
        /// Untargeted messages (channel All / empty, no version bounds) always match.
        /// </summary>
        public bool AppliesTo(string version, string channel)
        {
            return ChannelMatches(Channel, channel)
                   && VersionMatches(MinVersion, MaxVersion, version);
        }

        private static string NonEmpty(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;
            string trimmed = s.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static bool ChannelMatches(string target, string client)
        {
            List<string> targets = (target ?? "")
                .Split(',')
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => s.Length > 0)
                .ToList();

            if (targets.Count == 0 || targets.Exists(t => t == "all"))
                return true;

            string ch = NonEmpty(client)?.ToLowerInvariant();
            return ch != null && targets.Contains(ch);
        }

        private static bool VersionMatches(string min, string max, string client)
        {
            min = NonEmpty(min);
            max = NonEmpty(max);
            if (min == null && max == null)
                return true;

            string version = NonEmpty(client);
            if (version == null || version == "?")
                return false;

            if (min != null && CompareVersion(version, min) < 0)
                return false;
            if (max != null && CompareVersion(version, max) > 0)
                return false;
            return true;
        }

        /// <summary>Dotted numeric versions (`1.8` == `1.8.0`). A `-` / `+` suffix is ignored.</summary>
        private static int CompareVersion(string a, string b)
        {
            int[] pa = ParseVersion(a);
            int[] pb = ParseVersion(b);
            int len = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < len; i++)
            {
                int va = i < pa.Length ? pa[i] : 0;
                int vb = i < pb.Length ? pb[i] : 0;
                int cmp = va.CompareTo(vb);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

        private static int[] ParseVersion(string s)
        {
            string core = s.Split(['-', '+'], 2)[0];
            List<int> parts = [];
            foreach (string part in core.Split('.'))
            {
                int i = 0;
                while (i < part.Length && char.IsAsciiDigit(part[i]))
                    i++;
                if (i == 0)
                    continue;
                if (int.TryParse(part[..i], out int n))
                    parts.Add(n);
            }

            return [.. parts];
        }

        private DateTime? ConvertUnixTimeToDateTime(long? unixTime)
        {
            if (unixTime.HasValue)
            {
                try
                {
                    // Ensure the value is within the valid range for DateTimeOffset
                    return DateTimeOffset.FromUnixTimeSeconds(unixTime.Value).LocalDateTime;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Log or handle invalid timestamp value
                    return null;
                }
            }

            return null;
        }

        // Computed properties to convert Unix timestamp to DateTime
        public DateTime? CreatedAtDateTime => ConvertUnixTimeToDateTime(CreatedAt);

        public DateTime? StartDateDateTime => ConvertUnixTimeToDateTime(StartDate);
        public DateTime? EndDateDateTime => ConvertUnixTimeToDateTime(EndDate);
    }
}