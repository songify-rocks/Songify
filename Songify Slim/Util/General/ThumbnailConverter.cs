using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using SpotifyAPI.Web;

namespace Songify_Slim.Util.General;

public static class ThumbnailConverter
{
    public static async Task<Image> ConvertThumbnailAsync(IRandomAccessStreamReference thumbRef)
    {
        if (thumbRef == null) return null;

        using IRandomAccessStream stream = await thumbRef.OpenReadAsync();
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

        // Get width/height
        int width = (int)decoder.PixelWidth;
        int height = (int)decoder.PixelHeight;

        // Copy to byte[]
        byte[] bytes;
        using (var reader = new DataReader(stream.GetInputStreamAt(0)))
        {
            bytes = new byte[stream.Size];
            await reader.LoadAsync((uint)stream.Size);
            reader.ReadBytes(bytes);
        }

        // Convert to base64 data URL
        string base64 = Convert.ToBase64String(bytes);
        string url = $"data:image/png;base64,{base64}";

        return new Image
        {
            Width = width,
            Height = height,
            Url = url
        };
    }
}