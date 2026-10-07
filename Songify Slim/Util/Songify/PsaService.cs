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

            // Null means the request failed. An empty body is a successful "no notices" response.
            if (result == null)
                return null;

            if (string.IsNullOrWhiteSpace(result))
                return [];

            try
            {
                List<Psa> psas = JsonConvert.DeserializeObject<List<Psa>>(result) ?? [];
                string version = GlobalObjects.AppVersion;
                string channel = Settings.ReleaseChannel.ToString();
                return psas.Where(p => p.AppliesTo(version, channel)).ToList();
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Api, "Error getting PSAs", e);
                return null;
            }
        }
    }
}