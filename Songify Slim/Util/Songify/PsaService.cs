using Newtonsoft.Json;
using Songify_Slim.Models;
using Songify_Slim.Util.Songify.APIs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.General;

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
                return psas is { Count: > 0 } ? psas : null;
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
                return null;
            }
        }
    }
}