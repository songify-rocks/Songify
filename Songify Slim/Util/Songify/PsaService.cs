using Newtonsoft.Json;
using Songify_Slim.Models.Responses;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify
{
    internal static class PsaService
    {
        public static async Task<List<Psa>> GetPsaAsync()
        {
            string result = await SongifyApi.GetMotdAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(result))
                return null;

            try
            {
                List<Psa> psas = JsonConvert.DeserializeObject<List<Psa>>(result);
                if (psas is not { Count: > 0 })
                    return null;

                string version = GlobalObjects.AppVersion;
                string channel = Settings.ReleaseChannel.ToString();
                List<Psa> relevant = psas.Where(p => p.AppliesTo(version, channel)).ToList();
                return relevant.Count > 0 ? relevant : null;
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error getting PSAs", e);
                return null;
            }
        }
    }
}