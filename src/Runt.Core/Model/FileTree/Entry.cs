using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model.FileTree
{
    [JsonConverter(typeof(EntryJsonConverter))]
    public abstract class Entry
    {
        readonly string _relPath;
        readonly bool _isOpen;

        public Entry(string relativePath, bool isOpen)
        {
            Contract.Requires(_relPath != null);

            _relPath = relativePath;
            _isOpen = isOpen;
        }

        [JsonProperty("key")]
        public string RelativePath
        {
            get { return _relPath; }
        }

        public abstract Entry AsOpen(bool open, JObject changes);
        public abstract Entry WithChild(int index, Entry child, JObject changes, JObject subChange);

        protected void RegisterOpenChange(bool open, JObject c)
        {
            if (open)
            {
                Utils.RegisterChange(c, () => IsOpen, true, null);
                Utils.RegisterChange(c, () => Children, Children, null);
            }
            else
            {
                Utils.RegisterChange(c, () => IsOpen, false, null);
                Utils.RegisterChange(c, () => Children, new Entry[0], null);
            }
        }

        [JsonProperty("open")]
        public bool IsOpen
        {
            get { return _isOpen; }
        }

        [JsonProperty("cid")]
        public virtual string ContentId
        {
            get { return null; }
        }

        [JsonProperty("name")]
        public abstract string Name { get; }

        [JsonProperty("children")]
        public abstract IReadOnlyList<Entry> Children { get; }

        [JsonProperty("type")]
        public abstract string Type { get; }
    }
}
