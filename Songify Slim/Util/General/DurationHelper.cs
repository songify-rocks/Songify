using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    internal class DurationHelper
    {
        public static TimeSpan? ParseDuration(string duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
                return null;

            duration = duration.Trim();

            // Case 1: just seconds (e.g. "59")
            if (!duration.Contains(":"))
            {
                if (int.TryParse(duration, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds))
                    return TimeSpan.FromSeconds(seconds);

                return null;
            }

            // Case 2/3: "m:ss" or "h:mm:ss"
            string[] parts = duration.Split(':');

            return parts.Length switch
            {
                // m:ss
                2 when int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int seconds) =>
                    new TimeSpan(0, minutes, seconds),
                // h:mm:ss
                3 when int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes) &&
                       int.TryParse(parts[2], out int seconds) => new TimeSpan(hours, minutes, seconds),
                _ => null
            };
        }
    }
}