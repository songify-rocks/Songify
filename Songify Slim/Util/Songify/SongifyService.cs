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
        public static async Task UploadSong(string currSong, string coverUrl = null, Enums.RequestPlayerType playerType = Enums.RequestPlayerType.Other, string artist = "", string title = "", string requester = "")
        {
            try
            {
                dynamic payload = new
                {
                    uuid = Settings.Uuid,
                    key = Settings.AccessKey,
                    song = currSong,
                    cover = coverUrl,
                    song_id = GlobalObjects.CurrentSong?.SongId,
                    playertype = Enum.GetName(typeof(Enums.RequestPlayerType), playerType),
                    artist,
                    title,
                    requester
                };
                await SongifyApi.PostSongAsync(Json.Serialize(payload));
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error uploading Song information", e);
            }
        }

        public static async Task UploadHistory(string currSong, int unixTimestamp)
        {
            try
            {
                string song = GlobalObjects.CurrentSong == null ? currSong : $"{GlobalObjects.CurrentSong.Artists} - {GlobalObjects.CurrentSong.Title}";

                dynamic payload = new
                {
                    id = Settings.Uuid,
                    tst = unixTimestamp,
                    song,
                    key = Settings.AccessKey
                };
                await SongifyApi.PostHistoryAsync(Json.Serialize(payload));
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error uploading history information", e);
            }
        }
    }
}