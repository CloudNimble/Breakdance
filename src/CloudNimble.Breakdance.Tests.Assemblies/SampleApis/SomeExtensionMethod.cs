using System.Linq;

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
