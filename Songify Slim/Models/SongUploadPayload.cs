namespace Songify_Slim.Models
{
    /// <summary>
    /// JSON body for Songify API song upload. Property names match Swan.Json output from the previous anonymous payload (mixed casing).
    /// </summary>
    public sealed class SongUploadPayload
    {
        public string uuid { get; set; }
        public string key { get; set; }
        public string song { get; set; }
        public string cover { get; set; }
        public string song_id { get; set; }
        public string playertype { get; set; }
        public string Artists { get; set; }
        public string Title { get; set; }
        public string Requester { get; set; }
        public SongUploadNextPayload next { get; set; }
    }

    public sealed class SongUploadNextPayload
    {
        public int queueid { get; set; }
        public string trackid { get; set; }
        public string artist { get; set; }
        public string title { get; set; }
        public string length { get; set; }
        public string requester { get; set; }
        public string albumcover { get; set; }
    }
}
