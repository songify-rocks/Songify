using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web
{
    public static class Util
    {
        public static string GetStringAttribute<T>(this T en, string separator = "") where T : struct, IConvertible
        {
            Enum e = (Enum)(object)en;
            IEnumerable<StringAttribute> attributes =
              Enum.GetValues(typeof(T))
              .Cast<T>()
              .Where(v => e.HasFlag((Enum)(object)v))
              .Select(v => typeof(T).GetField(v.ToString(CultureInfo.InvariantCulture)))
              .Select(f => f.GetCustomAttributes(typeof(StringAttribute), false)[0])
              .Cast<StringAttribute>();

            List<string> list = [];
            attributes.ToList().ForEach(element => list.Add(element.Text));
            return string.Join(separator, list);
        }

        public static long ToUnixTimeMillisecondsPoly(this DateTime time)
        {
            return (long)time.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }

    public sealed class StringAttribute(string text) : Attribute
    {
        public string Text { get; set; } = text;
    }
}