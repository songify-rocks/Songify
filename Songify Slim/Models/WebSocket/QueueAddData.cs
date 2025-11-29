namespace Songify_Slim.Models.WebSocket
{
    public class QueueAddData
    {
        public string Track { get; set; }
        public string Requester { get; set; } = "WebSocket";
    }
}