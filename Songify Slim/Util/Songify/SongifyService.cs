using Songify_Slim.Models;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using Swan.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.Songify
{
    internal class SongifyService
    {
        public static async Task UploadSong(SongUploadPayload payload)
        {
            try
            {
                await SongifyApi.PostSongAsync(Json.Serialize(payload));
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error uploading Song information", e);
            }
        }
    }
}