using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Util.General
{
   public static class EnumHelper
    {
        public static string GetDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            if (fi?.GetCustomAttribute(typeof(DescriptionAttribute)) is DescriptionAttribute attr)
                return attr.Description;

            return value.ToString();
        }
    }
}
