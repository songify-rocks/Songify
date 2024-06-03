using System;
using System.Collections.Generic;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;

namespace Songify_Slim.Models
{
    public class TrackInfo : IEquatable<TrackInfo>
    {
        public string Artists { get; set; }
        public string Title { get; set; }
        public List<Image> Albums { get; set; }
        public string SongId { get; set; }
        public int DurationMs { get; set; }
        public bool IsPlaying { get; set; }
        public string Url { get; set; }
        public int DurationPercentage { get; set; }
        public int DurationTotal { get; set; }
        public int Progress { get; set; }
        public PlaylistInfo Playlist { get; set; }
        public List<SimpleArtist> FullArtists { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as TrackInfo);
        }

        public bool Equals(TrackInfo other)
        {
            return other != null &&
                   Albums == other.Albums &&
                   Artists == other.Artists &&
                   DurationMs == other.DurationMs &&
                   DurationPercentage == other.DurationPercentage &&
                   DurationTotal == other.DurationTotal &&
                   FullArtists == other.FullArtists &&
                   IsPlaying == other.IsPlaying &&
                   Playlist == other.Playlist &&
                   Progress == other.Progress &&
                   SongId == other.SongId &&
                   Title == other.Title &&
                   Url == other.Url;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (Albums != null ? Albums.GetHashCode() : 0);
            hash = hash * 31 + (Artists != null ? Artists.GetHashCode() : 0);
            hash = hash * 31 + DurationMs.GetHashCode();
            hash = hash * 31 + DurationPercentage.GetHashCode();
            hash = hash * 31 + DurationTotal.GetHashCode();
            hash = hash * 31 + (FullArtists != null ? FullArtists.GetHashCode() : 0);
            hash = hash * 31 + IsPlaying.GetHashCode();
            hash = hash * 31 + (Playlist != null ? Playlist.GetHashCode() : 0);
            hash = hash * 31 + Progress.GetHashCode();
            hash = hash * 31 + (SongId != null ? SongId.GetHashCode() : 0);
            hash = hash * 31 + (Title != null ? Title.GetHashCode() : 0);
            hash = hash * 31 + (Url != null ? Url.GetHashCode() : 0);
            return hash;
        }

        public static bool operator ==(TrackInfo left, TrackInfo right)
        {
            return EqualityComparer<TrackInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(TrackInfo left, TrackInfo right)
        {
            return !(left == right);
        }
    }

    public class PlaylistInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Owner { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
    }
}