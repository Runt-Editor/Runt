using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model.FileTree
{
    public abstract class Entry
    {
        readonly string _relPath;

        public Entry(string relativePath)
        {
            _relPath = relativePath;
        }

        [JsonIgnore]
        public string RelativePath
        {
            get { return _relPath; }
        }

        public abstract Entry WithChild(int index, Entry child, JObject changes, JObject subChange);

        [JsonProperty("name")]
        public abstract string Name { get; }

        [JsonProperty("children")]
        public abstract IReadOnlyList<Entry> Children { get; }

        [JsonProperty("type")]
        public abstract string Type { get; }

        [JsonProperty("key")]
        public abstract string Key { get; }
    }
}
