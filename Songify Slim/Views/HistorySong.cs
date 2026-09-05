namespace Songify_Slim.Views;

/// <summary>Legacy history song row model used by HistoryViewModel delete/export paths.</summary>
public class Song
{
    public string Time { get; set; }
    public string Name { get; set; }
    public long UnixTimeStamp { get; set; }
}
