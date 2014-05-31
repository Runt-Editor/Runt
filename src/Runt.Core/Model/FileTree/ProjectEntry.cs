using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Runt.Core.Model.FileTree
{
    using Newtonsoft.Json.Linq;
    using Runt.DesignTimeHost;
    using IOPath = Path;

    public class ProjectEntry : DirectoryEntry
    {
        readonly ReferencesEntry _references;
        protected readonly Lazy<IReadOnlyList<Entry>> _children;
        readonly int _id;

        protected ProjectEntry(string rel, DirectoryInfo dir, ImmutableList<DirectoryEntry> directories,
            ImmutableList<Entry> files, ReferencesEntry references,
            int id)
            : base(rel, dir, directories, files)
        {
            _id = id;
            _references = references;
            _children = new Lazy<IReadOnlyList<Entry>>(() => ImmutableList.Create<Entry>(_references).AddRange(base.Children));
        }

        public static new Tuple<DirectoryEntry, ImmutableList<ProjectEntry>> Create(DirectoryInfo dir, string relativePath)
        {
            var dirsAndProjects = from d in dir.EnumerateDirectories()
                                  where !d.IsHidden()
                                  && !d.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) 
                                  && !d.Name.Equals("obj", StringComparison.OrdinalIgnoreCase)
                                  orderby d.Name
                                  select Create(d, IOPath.Combine(relativePath, d.Name));

            var dirs = ImmutableList.CreateBuilder<DirectoryEntry>();
            foreach (var dap in dirsAndProjects)
            {
                // Discard sub-projects
                dirs.Add(dap.Item1);
            }

            var files = from f in dir.EnumerateFiles()
                        where !f.IsHidden()
                        orderby f.Name
                        select (Entry)FileEntry.Create(f, IOPath.Combine(relativePath, f.Name));

            var project = new ProjectEntry(relativePath, dir, dirs.ToImmutable(), files.ToImmutableList(), 
                new ReferencesEntry(dir.FullName, ImmutableList.Create<ReferenceEntry>()), -1);
            return new Tuple<DirectoryEntry, ImmutableList<ProjectEntry>>(
                project,
                ImmutableList.Create(project));
        }

        public ProjectEntry WithId(int id, JObject change)
        {
            Utils.RegisterChange(change, () => Id, id, null);
            return new ProjectEntry(RelativePath, _dir, _directories, _files, _references, id);
        }

        public override Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
        {
            if (index == 0)
                throw new InvalidOperationException("Cannot set the references child of a project");
            var lists = ChangeIndex(index - 1, child, changes, subChange);
            return new ProjectEntry(RelativePath, _dir, lists.Item1, lists.Item2, _references, _id);
        }

        public ProjectEntry WithReferences(ReferencesEventArgs e, JObject c)
        {
            Dictionary<string, ReferenceEntry> cache = new Dictionary<string, ReferenceEntry>();
            Func<string, ReferenceEntry> lookup = null;
            lookup = name =>
            {
                ReferenceEntry value;
                if(!cache.TryGetValue(name, out value))
                {
                    var package = e.Packages[name];
                    var deps = package.Dependencies.Select(d => lookup(d.Name)).ToImmutableList();
                    value = cache[name] = new ReferenceEntry(package.Name, package.Version, package.Unresolved, deps);
                }
                return value;
            };

            var newRef = new ReferencesEntry(_dir.FullName, lookup(e.Root).Dependencies);
            var indexChange = new JObject();
            Utils.RegisterChange(indexChange, "0", newRef, null);

            // Note: I use null here because I don't want to create the lists.
            // given that indexChange will never be null, this is safe.
            Utils.RegisterChange(c, () => Children, null, indexChange);
            return new ProjectEntry(RelativePath, _dir, _directories, _files, newRef, _id);
        }

        [JsonIgnore]
        public bool Initialized
        {
            get { return _id != -1; }
        }

        [JsonIgnore]
        public int Id
        {
            get { return _id; }
        }

        [JsonIgnore]
        public string Path
        {
            get { return _dir.FullName; }
        }

        public override string Type
        {
            get { return "project"; }
        }

        public override IReadOnlyList<Entry> Children
        {
            get { return _children.Value; }
        }

        protected class ReferencesEntry : Entry
        {
            readonly string _key;
            readonly ImmutableList<ReferenceEntry> _references;

            public ReferencesEntry(string keyPrefix, ImmutableList<ReferenceEntry> references)
                : base(null)
            {
                _key = keyPrefix + ":references";
                _references = references;
            }

            public override Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
            {
                throw new InvalidOperationException("Cannot set children on a references node");
            }

            public override string Key
            {
                get { return _key; }
            }

            public override string Name
            {
                get { return "references"; }
            }

            public override string Type
            {
                get { return "reference"; }
            }

            public override IReadOnlyList<Entry> Children
            {
                get { return _references; }
            }
        }
    }
}
