namespace Songify_Slim.Models.WebSocket
{
    public class VolumeData
    {
        public int Value { get; set; }
    }

    public class PlayPlaylistData
    {
        public string playlist { get; set; }
        public bool Shuffle { get; set; }
    }
}