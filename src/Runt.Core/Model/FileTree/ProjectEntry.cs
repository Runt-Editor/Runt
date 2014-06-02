using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model.FileTree
{
    using IOPath = Path;

    public class ProjectEntry : DirectoryEntry
    {
        readonly ReferencesEntry _references;
        protected new readonly Lazy<IReadOnlyList<Entry>> _children;
        readonly int _id;
        readonly ImmutableList<DiagnosticMessage> _diagnostics;
        readonly ImmutableList<string> _sources;
        readonly ImmutableDictionary<string, string> _generatedSources;

        protected ProjectEntry(string rel, bool isOpen, DirectoryInfo dir, ImmutableList<DirectoryEntry> directories,
            ImmutableList<Entry> files, ReferencesEntry references, ImmutableList<DiagnosticMessage> diagnostics, int id,
            ImmutableList<string> sources, ImmutableDictionary<string, string> generatedSources)
            : base(rel, isOpen, dir, directories, files)
        {
            _id = id;
            _references = references;
            _children = new Lazy<IReadOnlyList<Entry>>(() => ImmutableList.Create<Entry>(_references).AddRange(base.Children));
            _diagnostics = diagnostics;
            _sources = sources;
            _generatedSources = generatedSources;
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

            var project = new ProjectEntry(relativePath, false, dir, dirs.ToImmutable(), files.ToImmutableList(),
                new ReferencesEntry(false, relativePath + ":references", ImmutableList.Create<ReferenceEntry>(), null),
                ImmutableList.Create<DiagnosticMessage>(), -1, ImmutableList.Create<string>(), ImmutableDictionary.Create<string, string>());
            return new Tuple<DirectoryEntry, ImmutableList<ProjectEntry>>(
                project,
                ImmutableList.Create(project));
        }

        public ProjectEntry WithId(int id, JObject change)
        {
            Utils.RegisterChange(change, () => Id, id, null);
            return new ProjectEntry(RelativePath, IsOpen, _dir, _directories, _files, _references, _diagnostics, id, _sources, _generatedSources);
        }

        public override Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
        {
            if (index == 0)
            {
                if (child is ReferencesEntry)
                    return WithReferences((ReferencesEntry)child, changes, subChange);
                else
                    throw new InvalidOperationException("Cannot set references entry of a project to a non-references entry");
            }

            var lists = ChangeIndex(index - 1, child, changes, subChange);
            return new ProjectEntry(RelativePath, IsOpen, _dir, lists.Item1, lists.Item2, _references, _diagnostics, _id, _sources, _generatedSources);
        }

        public override Entry AsOpen(bool open, JObject c)
        {
            RegisterOpenChange(open, c);
            return new ProjectEntry(RelativePath, open, _dir, _directories, _files, _references, _diagnostics, _id, _sources, _generatedSources);
        }

        private ProjectEntry WithReferences(ReferencesEntry newRef, JObject changes, JObject subChanges)
        {
            var indexChange = new JObject();
            Utils.RegisterChange(indexChange, "0", newRef, subChanges);

            // Note: I use null here because I don't want to create the lists.
            // given that indexChange will never be null, this is safe.
            Utils.RegisterChange(changes, () => Children, null, indexChange);
            return new ProjectEntry(RelativePath, IsOpen, _dir, _directories, _files, newRef, _diagnostics, _id, _sources, _generatedSources);
        }

        public ProjectEntry WithReferences(ReferencesEventArgs e, JObject c)
        {
            Func<string, string, ReferenceEntry> lookup = null;
            lookup = (name, rel) =>
            {
                var nrel = rel + ":" + name;
                var package = e.Packages[name];
                var deps = package.Dependencies.Select(d => lookup(d.Name, nrel)).ToImmutableList();
                return new ReferenceEntry(nrel, false, package.Name, package.Version, package.Unresolved, deps);
            };

            var newRef = new ReferencesEntry(_references.IsOpen, _references.RelativePath, lookup(e.Root, _references.RelativePath).Dependencies, e);
            return WithReferences(newRef, c, null);
        }

        public ProjectEntry WithDiagnostics(IImmutableList<string> errors, IImmutableList<string> warnings, JObject change)
        {
            // note that diagnostics are not exposed on a project level,
            // thus there is no call to RegisterChange
            var diag = errors.Union(warnings)
                .Select(DiagnosticMessage.Create).ToImmutableList();

            return new ProjectEntry(RelativePath, IsOpen, _dir, _directories, _files, _references,
                diag, _id, _sources, _generatedSources);
        }

        public ProjectEntry WithSources(SourcesEventArgs e)
        {
            return new ProjectEntry(RelativePath, IsOpen, _dir, _directories, _files, _references, _diagnostics, _id,
                e.Files, e.GeneratedFiles);
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

        [JsonIgnore]
        public ImmutableList<string> Sources
        {
            get { return _sources; }
        }

        [JsonIgnore]
        public ImmutableDictionary<string, string> GeneratedSources
        {
            get { return _generatedSources; }
        }

        [JsonIgnore]
        public ImmutableList<ReferencesEventArgs.Package> References
        {
            get { return _references.References; }
        }

        [JsonIgnore]
        public ImmutableList<DiagnosticMessage> Diagnostics
        {
            get { return _diagnostics; }
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
            readonly ImmutableList<ReferenceEntry> _references;
            readonly ReferencesEventArgs _raw;
            readonly Lazy<ImmutableList<ReferencesEventArgs.Package>> _ref;

            public ReferencesEntry(bool isOpen, string key, ImmutableList<ReferenceEntry> references, ReferencesEventArgs raw)
                : base(key, isOpen)
            {
                _references = references;
                _raw = raw;
                _ref = new Lazy<ImmutableList<ReferencesEventArgs.Package>>(GenerateReferences);
            }

            public override Entry WithChild(int index, Entry child, JObject changes, JObject subChange)
            {
                var indexChange = new JObject();
                Utils.RegisterChange(indexChange, index.ToString(), child, subChange);

                // Note: I use null here because I don't want to create the lists.
                // given that indexChange will never be null, this is safe.
                Utils.RegisterChange(changes, () => Children, null, indexChange);
                return new ReferencesEntry(IsOpen, RelativePath, _references.SetItem(index, (ReferenceEntry)child), _raw);
            }

            public override Entry AsOpen(bool open, JObject changes)
            {
                RegisterOpenChange(open, changes);
                return new ReferencesEntry(open, RelativePath, _references, _raw);
            }

            public override string Name
            {
                get { return "References"; }
            }

            public override string Type
            {
                get { return "reference"; }
            }

            public override IReadOnlyList<Entry> Children
            {
                get { return _references; }
            }

            [JsonIgnore]
            public ImmutableList<ReferencesEventArgs.Package> References
            {
                get { return _ref.Value; }
            }

            private ImmutableList<ReferencesEventArgs.Package> GenerateReferences()
            {
                if (_raw == null)
                    return null;

                var set = new Dictionary<string, ReferencesEventArgs.Package>();
                Action<string> walk = null;
                walk = name =>
                {
                    if (set.ContainsKey(name))
                        return;

                    var package = _raw.Packages[name];
                    set.Add(name, package);

                    foreach (var dep in package.Dependencies)
                        walk(dep.Name);
                };

                walk(_raw.Root);
                set.Remove(_raw.Root);

                return set.Values.ToImmutableList();
            }
        }
    }
}
