using System;

namespace Songify_Slim.Models.Spotify
{
    public class SpotifyPersistentIssue
    {
        /// <summary>
        /// Stable identity for de-duplication and dismissing.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Stable identifier for the issue type (e.g. "rate_limit", "quota", "unauthorized", "api_error").
        /// </summary>
        public string Kind { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        /// <summary>UTC timestamp for when the issue was created.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// If set: when Spotify is expected to work again (UTC).
        /// Used for rate limiting / quota cooldowns.
        /// </summary>
        public DateTime? RetryUntilUtc { get; set; }

        /// <summary>
        /// Optional TTL for non-cooldown issues. If set and in the past, the issue should be treated as stale.
        /// </summary>
        public DateTime? ExpiresAtUtc { get; set; }

        /// <summary>User dismissed the banner.</summary>
        public bool Dismissed { get; set; }

        /// <summary>
        /// Used to avoid showing duplicates. Keep it stable for "same issue again".
        /// </summary>
        public string DedupKey { get; set; }

        /// <summary>How many times this issue was observed (toast may be suppressed after the first).</summary>
        public int OccurrenceCount { get; set; } = 1;

        /// <summary>Most recent observation time (UTC).</summary>
        public DateTime LastSeenAtUtc { get; set; }

        public bool IsStale(DateTime utcNow)
        {
            if (Dismissed)
                return true;

            if (ExpiresAtUtc is { } exp && exp <= utcNow)
                return true;

            return false;
        }
    }
}

