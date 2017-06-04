using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public static class SomeExtensionMethod
    {

        public static string ToBetterString<T>(this List<T> list) where T : class
        {
            return string.Join(", ", list.Select(c => c.ToString()));
        }

    }
}
