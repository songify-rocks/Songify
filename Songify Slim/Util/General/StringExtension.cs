using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Songify_Slim.Util.General
{
    public static class StringExtension
    {
        public static string Format(this string str, params Expression<Func<string, object>>[] args)
        {
            var parameters = args.ToDictionary(e => string.Format("{{{0}}}", e.Parameters[0].Name),
                e => e.Compile()(e.Parameters[0].Name));

            StringBuilder sb = new StringBuilder(str);
            foreach (var kv in parameters) sb.Replace(kv.Key, kv.Value != null ? kv.Value.ToString() : "");

            return sb.ToString();
        }
    }
}