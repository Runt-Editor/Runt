using System.IO;

namespace Runt.Core
{
    static class IOExtensions
    {
        public static bool IsHidden(this FileSystemInfo fsInfo)
        {
            return (fsInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
        }
    }
}
