using System;
using System.Collections.Immutable;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model.FileTree
{
    class PackagesEntry : DirectoryEntry
    {
        protected PackagesEntry(DirectoryInfo dir, ImmutableList<DirectoryEntry> directories)
            : base("packages", dir, directories, ImmutableList.Create<Entry>())
        {

        }

        public override Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
        {
            throw new InvalidOperationException("Cannot set children on package directories");
        }

        public static PackagesEntry Create(DirectoryInfo dir)
        {
            // TODO: Implement
            return new PackagesEntry(dir, ImmutableList.Create<DirectoryEntry>());
        }

        public override string Type
        {
            get { return "packages"; }
        }
    }
}
