using Songify_Slim.Util.Songify.APIs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify
{
    internal static class CanvasService
    {
        public static async Task<Tuple<bool, string>> GetCanvasAsync(string songId)
        {
            if (string.IsNullOrEmpty(songId))
            {
                return new Tuple<bool, string>(false, "");
            }

            string result = await SongifyApi.GetCanvasRawAsync(songId).ConfigureAwait(false);

            // API sends quoted string, so remove quotes
            result = result.Replace("\"", "");

            return result == "No canvas found" ? new Tuple<bool, string>(false, "") : new Tuple<bool, string>(true, result);
        }
    }
}