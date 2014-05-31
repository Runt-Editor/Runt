using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Runt.Core.Model.FileTree
{
    class FileEntry : Entry
    {
        readonly FileInfo _file;

        public FileEntry(FileInfo file)
        {
            _file = file;
        }

        public static FileEntry Create(FileInfo file, string relativePath)
        {
            return new FileEntry(file);
        }

        public override string Name
        {
            get { return _file.Name; }
        }

        public override IEnumerable<Entry> Children
        {
            get { return Enumerable.Empty<Entry>(); }
        }
    }
}
