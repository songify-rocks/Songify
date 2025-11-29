using System.Collections.Generic;

namespace Songify_Slim.Models.Pear
{
    internal class PearQueue
    {
        public List<Item> Items { get; set; }
        public bool AutoPlaying { get; set; }
        public string Continuation { get; set; }
    }

    public partial class Item
    {
        public PlaylistPanelVideoWrapperRenderer PlaylistPanelVideoWrapperRenderer { get; set; }
    }

    public class PlaylistPanelVideoWrapperRenderer
    {
        public PrimaryRenderer PrimaryRenderer { get; set; }
        public List<Counterpart> Counterpart { get; set; }
    }

    public class PrimaryRenderer
    {
        public PlaylistPanelVideoRenderer PlaylistPanelVideoRenderer { get; set; }
    }

    public class Counterpart
    {
        public CounterpartRenderer CounterpartRenderer { get; set; }
    }

    public class CounterpartRenderer
    {
        public PlaylistPanelVideoRenderer PlaylistPanelVideoRenderer { get; set; }
        public SegmentMap SegmentMap { get; set; }
    }

    public class SegmentMap
    {
        public List<Segment> Segment { get; set; }
    }

    public class Segment
    {
        public string PrimaryVideoStartTimeMilliseconds { get; set; }
        public string CounterpartVideoStartTimeMilliseconds { get; set; }
        public string DurationMilliseconds { get; set; }
    }

    public class PlaylistPanelVideoRenderer
    {
        public TextContainer Title { get; set; }
        public TextContainer LongBylineText { get; set; }
        public Thumbnail Thumbnail { get; set; }
        public LengthText LengthText { get; set; }
        public bool Selected { get; set; }
        public NavigationEndpoint NavigationEndpoint { get; set; }
        public string VideoId { get; set; }
        public TextContainer ShortBylineText { get; set; }
        public string TrackingParams { get; set; }
        public Menu Menu { get; set; }
        public string PlaylistSetVideoId { get; set; }
        public bool CanReorder { get; set; }
        public string PlaylistEditParams { get; set; }
        public QueueNavigationEndpoint QueueNavigationEndpoint { get; set; }
        public List<Badge> Badges { get; set; } // Optional
    }

    public class TextContainer
    {
        public List<TextRun> Runs { get; set; }
    }

    public class TextRun
    {
        public string Text { get; set; }
    }

    public partial class Thumbnail
    {
        public List<ThumbnailItem> Thumbnails { get; set; }
    }

    public class ThumbnailItem
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class LengthText
    {
        public List<TextRun> Runs { get; set; }
        public Accessibility Accessibility { get; set; }
    }

    public partial class Accessibility
    {
        public AccessibilityData AccessibilityData { get; set; }
    }

    public partial class AccessibilityData
    {
        public string Label { get; set; }
    }

    public partial class NavigationEndpoint
    {
        public WatchEndpoint WatchEndpoint { get; set; }
    }

    public partial class WatchEndpoint
    {
        public string VideoId { get; set; }
        public string PlaylistId { get; set; }
        public int Index { get; set; }
        public string Params { get; set; }
        public string PlayerParams { get; set; }
        public string PlaylistSetVideoId { get; set; }
        public LoggingContext LoggingContext { get; set; }
    }

    public partial class LoggingContext
    {
        public VssLoggingContext VssLoggingContext { get; set; }
    }

    public partial class VssLoggingContext
    {
        public string SerializedContextData { get; set; }
    }

    public partial class Menu
    {
        public MenuRenderer MenuRenderer { get; set; }
    }

    public partial class MenuRenderer
    {
        public List<object> Items { get; set; } // You can replace `object` with specific types if you plan to use them
        public string TrackingParams { get; set; }
        public Accessibility Accessibility { get; set; }
    }

    public class QueueNavigationEndpoint
    {
        public QueueAddEndpoint QueueAddEndpoint { get; set; }
    }

    public partial class QueueAddEndpoint
    {
        public QueueTarget QueueTarget { get; set; }
        public string QueueInsertPosition { get; set; }
    }

    public partial class QueueTarget
    {
        public string VideoId { get; set; }
        public string BackingQueuePlaylistId { get; set; }
    }

    public partial class Badge
    {
        public MusicInlineBadgeRenderer MusicInlineBadgeRenderer { get; set; }
    }

    public partial class MusicInlineBadgeRenderer
    {
        public string TrackingParams { get; set; }
        public Icon Icon { get; set; }
        public Accessibility AccessibilityData { get; set; }
    }

    public partial class Icon
    {
        public string IconType { get; set; }
    }
}