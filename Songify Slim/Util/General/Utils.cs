using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
    internal static class Utils
    {
        public static bool IsDefault<T>(T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }
    }
}
