using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Songify_Slim.Util.General
{
    public static class StringExtension
    {
        public static string Format(this string str, params Expression<Func<string, object>>[] args)
        {
            StringBuilder sb = new(str);

            try
            {
                Dictionary<string, object> parameters = args.ToDictionary(e => $"{{{e.Parameters[0].Name}}}",
                    e => e.Compile()(e.Parameters[0].Name));

                foreach (KeyValuePair<string, object> kv in parameters) sb.Replace(kv.Key, kv.Value != null ? kv.Value.ToString() : "");
                return sb.ToString();

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return sb.ToString();

        }
    }
}