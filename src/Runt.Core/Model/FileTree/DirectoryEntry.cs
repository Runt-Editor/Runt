using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Runt.Core.Model.FileTree
{
    class DirectoryEntry : Entry
    {
        readonly DirectoryInfo _dir;
        readonly ImmutableList<DirectoryEntry> _directories;
        readonly ImmutableList<Entry> _files;

        protected DirectoryEntry(DirectoryInfo dir, ImmutableList<DirectoryEntry> directories, ImmutableList<Entry> files)
        {
            _dir = dir;
            _directories = directories;
            _files = files;
        }

        public static DirectoryEntry Create(DirectoryInfo dir, string relativePath)
        {
            var rel = Path.Combine(relativePath, dir.Name);
            if (relativePath == string.Empty && dir.Name == "packages")
                return PackagesEntry.Create(dir);

            var dirs = from d in dir.EnumerateDirectories()
                       where !d.IsHidden()
                       orderby d.Name
                       select Create(d, rel);

            var files = from f in dir.EnumerateFiles()
                        where !f.IsHidden()
                        orderby f.Name
                        select (Entry)FileEntry.Create(f, rel);

            return new DirectoryEntry(dir, dirs.ToImmutableList(), files.ToImmutableList());
        }

        public override string Name
        {
            get { return _dir.Name; }
        }

        public override IEnumerable<Entry> Children
        {
            get { return _directories.Cast<Entry>().Concat(_files); }
        }
    }
}
