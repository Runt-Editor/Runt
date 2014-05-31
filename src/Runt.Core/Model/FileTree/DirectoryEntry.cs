using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model.FileTree
{
    public class DirectoryEntry : Entry
    {
        protected readonly DirectoryInfo _dir;
        protected readonly ImmutableList<DirectoryEntry> _directories;
        protected readonly ImmutableList<Entry> _files;
        protected readonly Lazy<IReadOnlyList<Entry>> _children;

        protected DirectoryEntry(string rel, DirectoryInfo dir, ImmutableList<DirectoryEntry> directories, ImmutableList<Entry> files)
            : base(rel)
        {
            _dir = dir;
            _directories = directories;
            _files = files;

            _children = new Lazy<IReadOnlyList<Entry>>(() => _directories.Cast<Entry>().Concat(_files).ToImmutableList());
        }

        public static Tuple<DirectoryEntry, ImmutableList<ProjectEntry>> Create(DirectoryInfo dir, string relativePath)
        {
            if (relativePath == "packages")
                return new Tuple<DirectoryEntry, ImmutableList<ProjectEntry>>(PackagesEntry.Create(dir), null);

            if (relativePath != null && File.Exists(Path.Combine(dir.FullName, "project.json")))
                return ProjectEntry.Create(dir, relativePath);

            var dirsAndProjects = from d in dir.EnumerateDirectories()
                                  where !d.IsHidden()
                                  orderby d.Name
                                  select Create(d, Path.Combine(relativePath, d.Name));

            var dirs = ImmutableList.CreateBuilder<DirectoryEntry>();
            var projects = ImmutableList.CreateBuilder<ProjectEntry>();
            foreach(var dap in dirsAndProjects)
            {
                if (dap.Item2 != null)
                    projects.AddRange(dap.Item2);
                dirs.Add(dap.Item1);
            }

            var files = from f in dir.EnumerateFiles()
                        where !f.IsHidden()
                        orderby f.Name
                        select (Entry)FileEntry.Create(f, Path.Combine(relativePath, f.Name));

            return new Tuple<DirectoryEntry, ImmutableList<ProjectEntry>>(
                new DirectoryEntry(relativePath, dir,
                    dirs.ToImmutable(), files.ToImmutableList()),
                projects.ToImmutable());
        }

        public override Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
        {
            var lists = ChangeIndex(index, child, changes, subChange);
            return new DirectoryEntry(RelativePath, _dir, lists.Item1, lists.Item2);
        }

        protected Tuple<ImmutableList<DirectoryEntry>, ImmutableList<Entry>> ChangeIndex(
            int index, Entry newVal, JObject changes, JObject subChanges)
        {
            var indexChange = new JObject();
            Utils.RegisterChange(indexChange, index.ToString(), newVal, subChanges);

            // Note: I use null here because I don't want to create the lists.
            // given that indexChange will never be null, this is safe.
            Utils.RegisterChange(changes, () => Children, null, indexChange);

            if (index < _directories.Count)
            {
                var newDirs = _directories.SetItem(index, (DirectoryEntry)newVal);
                return new Tuple<ImmutableList<DirectoryEntry>, ImmutableList<Entry>>(newDirs, _files);
            }
            else
            {
                var newFiles = _files.SetItem(index - _directories.Count, newVal);
                return new Tuple<ImmutableList<DirectoryEntry>, ImmutableList<Entry>>(_directories, newFiles);
            }
        }

        public override string Name
        {
            get { return _dir.Name; }
        }

        public override IReadOnlyList<Entry> Children
        {
            get { return _children.Value; }
        }

        public override string Type
        {
            get { return "dir"; }
        }

        public override string Key
        {
            get { return Type + ":" + _dir.FullName; }
        }
    }
}
