using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;

namespace Songify_Slim.Util.Songify.Twitch
{
    /// <summary>
    /// Tracks which channel-point rewards were created by Songify's Client-Id.
    /// Twitch only lets the creating app update a reward or its redemptions (refund, cost, pause),
    /// so every mutation must be gated on this to avoid 403 Forbidden responses.
    /// </summary>
    internal static class TwitchManagedRewards
    {
        private static readonly object Gate = new();
        private static readonly SemaphoreSlim LoadLock = new(1, 1);
        private static readonly HashSet<string> ManagedIds = new(StringComparer.Ordinal);

        // Broadcaster the cached set belongs to. A different (or no) main account invalidates it.
        private static string _ownerId;

        private static bool IsLoadedFor(string broadcasterId)
        {
            lock (Gate)
                return _ownerId != null && _ownerId == broadcasterId;
        }

        /// <summary>Replace the cached set with ids Twitch reported as manageable for the current broadcaster.</summary>
        public static void Set(IEnumerable<string> rewardIds)
        {
            string ownerId = Settings.TwitchUser?.Id;
            if (string.IsNullOrWhiteSpace(ownerId))
                return;

            lock (Gate)
            {
                ManagedIds.Clear();
                foreach (string id in rewardIds ?? [])
                    if (!string.IsNullOrWhiteSpace(id))
                        ManagedIds.Add(id);
                _ownerId = ownerId;
            }
        }

        /// <summary>Record a reward Songify just created.</summary>
        public static void Remember(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                return;

            lock (Gate)
                ManagedIds.Add(rewardId);
        }

        /// <summary>Cached lookup only. Returns false when the set has not been loaded yet.</summary>
        public static bool IsManaged(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || TwitchPowerUpClient.IsKnown(rewardId))
                return false;

            lock (Gate)
                return _ownerId != null && _ownerId == Settings.TwitchUser?.Id && ManagedIds.Contains(rewardId);
        }

        /// <summary>Like <see cref="IsManaged"/>, but loads the set from Twitch first if needed.</summary>
        public static async Task<bool> IsManagedAsync(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || TwitchPowerUpClient.IsKnown(rewardId))
                return false;

            await EnsureLoadedAsync().ConfigureAwait(false);
            return IsManaged(rewardId);
        }

        /// <summary>Returns only the ids Songify may mutate.</summary>
        public static async Task<List<string>> FilterManagedAsync(IEnumerable<string> rewardIds)
        {
            List<string> ids = rewardIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? [];
            if (ids.Count == 0)
                return ids;

            await EnsureLoadedAsync().ConfigureAwait(false);
            return ids.Where(IsManaged).ToList();
        }

        public static async Task RefreshAsync()
        {
            string broadcasterId = Settings.TwitchUser?.Id;
            if (TwitchHandler.TwitchApi == null || string.IsNullOrWhiteSpace(broadcasterId))
                return;

            try
            {
                GetCustomRewardsResponse resp = await TwitchHandler.TwitchApi.Helix.ChannelPoints.GetCustomRewardAsync(
                    broadcasterId: broadcasterId,
                    onlyManageableRewards: true,
                    accessToken: Settings.TwitchAccessToken).ConfigureAwait(false);
                Set(resp?.Data?.Select(r => r.Id) ?? []);
            }
            catch (Exception e)
            {
                // Leave the set unloaded so the next mutation retries; until then nothing is mutated.
                Logger.Error(LogSource.Twitch, "Could not load Songify-managed rewards.", e);
            }
        }

        private static async Task EnsureLoadedAsync()
        {
            if (IsLoadedFor(Settings.TwitchUser?.Id))
                return;

            await LoadLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsLoadedFor(Settings.TwitchUser?.Id))
                    await RefreshAsync().ConfigureAwait(false);
            }
            finally
            {
                LoadLock.Release();
            }
        }
    }
}
