using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runt.Core.Model.FileTree;

namespace Runt.Core.Model
{
    public class Workspace
    {
        readonly DirectoryInfo _dir;
        readonly Entry _tree;
        readonly ImmutableList<string> _projects;
        readonly Lazy<ImmutableList<ProjectEntry>> _projectCache;

        private Workspace(DirectoryInfo dir, Entry tree,
            ImmutableList<string> projects)
        {
            _dir = dir;
            _tree = tree;
            _projects = projects;
            _projectCache = new Lazy<ImmutableList<ProjectEntry>>(() => _projects.Select(p => FindProject(_tree, p)).ToImmutableList());
        }

        private static ProjectEntry FindProject(Entry entry, string relPath)
        {
            for (int i = 0, l = entry.Children.Count; i < l; i++)
            {
                var child = entry.Children[i];
                if (relPath.Equals(child.RelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    // found the node. Update and recurse back
                    return (ProjectEntry)child;

                }
                else if (relPath.StartsWith(child.RelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    // found a parent of the node in question, recurse downwards
                    return FindProject(child, relPath);
                }
            }

            throw new InvalidOperationException("Project with given path not found");
        }

        public static Workspace Create(string path)
        {
            var dir = new DirectoryInfo(path);
            var tree = DirectoryEntry.Create(dir, string.Empty);
            return new Workspace(new DirectoryInfo(path), tree.Item1, tree.Item2.Select(p => p.RelativePath).ToImmutableList());
        }

        public Workspace WithContent(Entry content, JObject changes, JObject partials)
        {
            Utils.RegisterChange(changes, () => Content, content, partials);
            return new Workspace(_dir, content, _projects);
        }

        [JsonIgnore]
        public ImmutableList<ProjectEntry> Projects
        {
            get { return _projectCache.Value; }
        }

        [JsonProperty("path")]
        public string Path
        {
            get { return _dir.FullName; }
        }

        [JsonProperty("name")]
        public string Name
        {
            get { return _dir.Name; }
        }

        [JsonProperty("content")]
        public Entry Content
        {
            get { return _tree; }
        }
    }
}
