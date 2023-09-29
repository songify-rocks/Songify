namespace Songify_Core.Models
{
    internal class QueueItem
    {
        public int Queueid { get; set; }
        public string Uuid { get; set; }
        public string Trackid { get; set; }
        public string Artist { get; set; }
        public string Titel { get; set; }
        public string Length { get; set; }
        public string Requester { get; set; }
        public int Played { get; set; }
    }
}
