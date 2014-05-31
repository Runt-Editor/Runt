using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model.FileTree
{
    class FileEntry : Entry
    {
        readonly FileInfo _file;

        public FileEntry(string rel, bool isOpen, FileInfo file)
            : base(rel, isOpen)
        {
            _file = file;
        }

        public static FileEntry Create(FileInfo file, string relativePath)
        {
            return new FileEntry(relativePath, false, file);
        }

        public override sealed Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
        {
            throw new InvalidOperationException("Cannot set children on files");
        }

        public override Entry AsOpen(bool open, JObject changes)
        {
            RegisterOpenChange(open, changes);
            return new FileEntry(RelativePath, open, _file);
        }

        public override string Name
        {
            get { return _file.Name; }
        }

        public override IReadOnlyList<Entry> Children
        {
            get { return ImmutableList.Create<Entry>(); }
        }

        public override string Type
        {
            get { return "file"; }
        }
    }
}
