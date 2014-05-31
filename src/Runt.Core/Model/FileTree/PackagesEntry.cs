using System.Collections.Immutable;
using System.IO;

namespace Runt.Core.Model.FileTree
{
    class PackagesEntry : DirectoryEntry
    {
        protected PackagesEntry(DirectoryInfo dir, ImmutableList<DirectoryEntry> directories)
            : base(dir, directories, ImmutableList.Create<Entry>())
        {

        }

        public static PackagesEntry Create(DirectoryInfo dir)
        {
            // TODO: Implement
            return new PackagesEntry(dir, ImmutableList.Create<DirectoryEntry>());
        }
    }
}
