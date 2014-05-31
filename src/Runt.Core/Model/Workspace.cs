using System;
using System.IO;
using Newtonsoft.Json;
using Runt.Core.Model.FileTree;

namespace Runt.Core.Model
{
    public class Workspace
    {
        readonly Action<Func<Workspace, Workspace>> _update;
        readonly DirectoryInfo _dir;
        readonly Entry _tree;

        private Workspace(DirectoryInfo dir, Entry tree, Action<Func<Workspace, Workspace>> update)
        {
            _dir = dir;
            _update = update;
            _tree = tree;
        }

        public static Workspace Create(string path, Action<Func<Workspace, Workspace>> update)
        {
            var dir = new DirectoryInfo(path);
            var tree = DirectoryEntry.Create(dir, string.Empty);
            return new Workspace(new DirectoryInfo(path), tree, update);
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
