using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Runt
{
    internal static class Helpers
    {
        public static IEnumerable<T> RemoveHidden<T>(this IEnumerable<T> list)
            where T : FileSystemInfo
        {
            return list.Where(info => (info.Attributes & FileAttributes.Hidden) == 0);
        }

        public static T GetCustomAttribute<T>(this ICustomAttributeProvider provider, bool inherit = false)
        {
            return provider.GetCustomAttributes(typeof(T), inherit).Cast<T>().SingleOrDefault();
        }
    }
}
