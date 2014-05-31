using System.Collections.Generic;
using Newtonsoft.Json;

namespace Runt.Core.Model.FileTree
{
    public abstract class Entry
    {
        [JsonProperty("name")]
        public abstract string Name { get; }

        [JsonProperty("children")]
        public abstract IEnumerable<Entry> Children { get; }
    }
}
