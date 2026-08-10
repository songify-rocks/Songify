using System;
using System.Collections.Generic;
using System.Linq;
using Songify_Slim.Models.Blocklist;
using static Songify_Slim.Util.General.Enums;

namespace Songify_Slim.Util.Configuration;

/// <summary>
/// Keeps blocked-artist lookups cheap and memory light.
/// Full <see cref="BlockedArtist"/> objects (~thousands) live on disk; RAM only holds ID/name keys
/// for song-request checks. The full list is loaded into <see cref="Settings.CurrentConfig"/> only
/// while something is editing it, then unloaded again.
/// </summary>
internal static class ArtistBlocklistStore
{
    private static readonly object Gate = new();
    private static HashSet<string> _blockedIds = new(StringComparer.Ordinal);
    private static HashSet<string> _blockedNames = new(StringComparer.OrdinalIgnoreCase);
    private static bool _fullListResident;
    private static int _count;

    public static int Count
    {
        get { lock (Gate) return _count; }
    }

    public static bool IsFullListResident
    {
        get { lock (Gate) return _fullListResident; }
    }

    /// <summary>
    /// Build lookup indexes from a just-loaded config, then drop the full list from memory.
    /// Call after startup <see cref="ConfigHandler.ReadConfig"/> / import (once the YAML has been written).
    /// </summary>
    public static void InitializeFromConfigAndUnload()
    {
        lock (Gate)
        {
            List<BlockedArtist> artists = Settings.CurrentConfig.BlockedSpotifyArtists?.Artists;
            RebuildIndexes_NoLock(artists);
            ClearResidentList_NoLock();
        }
    }

    /// <summary>
    /// Ensure <see cref="Settings.CurrentConfig.BlockedSpotifyArtists.Artists"/> holds the full list from disk.
    /// </summary>
    public static List<BlockedArtist> EnsureLoaded()
    {
        lock (Gate)
        {
            Settings.CurrentConfig.BlockedSpotifyArtists ??= new BlockedSpotifyArtists();

            if (_fullListResident && Settings.CurrentConfig.BlockedSpotifyArtists.Artists != null)
                return Settings.CurrentConfig.BlockedSpotifyArtists.Artists;

            BlockedSpotifyArtists fromDisk = ConfigHandler.LoadBlockedSpotifyArtists();
            List<BlockedArtist> artists = fromDisk?.Artists ?? [];
            Settings.CurrentConfig.BlockedSpotifyArtists.Artists = artists;
            RebuildIndexes_NoLock(artists);
            _fullListResident = true;
            return artists;
        }
    }

    /// <summary>
    /// Snapshot for UI / export. Does not keep the config-resident list unless it was already loaded.
    /// </summary>
    public static List<BlockedArtist> LoadCopy()
    {
        lock (Gate)
        {
            if (_fullListResident && Settings.CurrentConfig.BlockedSpotifyArtists?.Artists != null)
                return Settings.CurrentConfig.BlockedSpotifyArtists.Artists.ToList();

            BlockedSpotifyArtists fromDisk = ConfigHandler.LoadBlockedSpotifyArtists();
            List<BlockedArtist> artists = fromDisk?.Artists ?? [];
            RebuildIndexes_NoLock(artists);
            return artists.ToList();
        }
    }

    /// <summary>
    /// Persist list, refresh indexes, then unload the full objects from <see cref="Settings.CurrentConfig"/>.
    /// </summary>
    public static void ReplaceAndUnload(List<BlockedArtist> artists)
    {
        artists ??= [];
        lock (Gate)
        {
            Settings.CurrentConfig.BlockedSpotifyArtists ??= new BlockedSpotifyArtists();
            Settings.CurrentConfig.BlockedSpotifyArtists.Artists = artists;
            ConfigHandler.WriteConfig(ConfigTypes.BlockedSpotifyArtists, Settings.CurrentConfig.BlockedSpotifyArtists);
            RebuildIndexes_NoLock(artists);
            ClearResidentList_NoLock();
        }
    }

    /// <summary>
    /// Drop full artist objects from config memory. Lookup indexes stay.
    /// </summary>
    public static void Unload()
    {
        lock (Gate)
        {
            ClearResidentList_NoLock();
        }
    }

    public static bool IsArtistBlocked(IEnumerable<(string Id, string Name)> trackArtists, out string matchedName)
    {
        matchedName = "";
        if (trackArtists == null) return false;

        lock (Gate)
        {
            foreach ((string id, string name) in trackArtists)
            {
                if (!string.IsNullOrEmpty(id) && _blockedIds.Contains(id))
                {
                    matchedName = name ?? "";
                    return true;
                }

                if (!string.IsNullOrEmpty(name) && _blockedNames.Contains(name))
                {
                    matchedName = name;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// True when WriteAllConfig must not serialize BlockedSpotifyArtists from CurrentConfig
    /// (the in-memory list was unloaded; disk is the source of truth).
    /// </summary>
    public static bool ShouldSkipConfigWrite()
    {
        lock (Gate)
        {
            return !_fullListResident;
        }
    }

    private static void RebuildIndexes_NoLock(IEnumerable<BlockedArtist> artists)
    {
        _blockedIds = new HashSet<string>(StringComparer.Ordinal);
        _blockedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int n = 0;
        if (artists != null)
        {
            foreach (BlockedArtist a in artists)
            {
                if (a == null) continue;
                n++;
                if (!string.IsNullOrEmpty(a.Id))
                    _blockedIds.Add(a.Id);
                if (!string.IsNullOrEmpty(a.Name))
                    _blockedNames.Add(a.Name);
            }
        }

        _count = n;
    }

    private static void ClearResidentList_NoLock()
    {
        if (Settings.CurrentConfig.BlockedSpotifyArtists != null)
            Settings.CurrentConfig.BlockedSpotifyArtists.Artists = [];
        _fullListResident = false;
        // Indexes and _count intentionally kept.
    }
}
