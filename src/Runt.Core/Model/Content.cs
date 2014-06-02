using Newtonsoft.Json;

namespace Runt.Core.Model
{
    public abstract class Content
    {
        readonly string _relativePath;
        readonly string _contentId;
        readonly bool _dirty;

        public Content(string contentId, string relativePath, bool dirty)
        {
            _contentId = contentId;
            _relativePath = relativePath;
            _dirty = dirty;
        }

        [JsonIgnore]
        public string RelativePath
        {
            get { return _relativePath; }
        }

        [JsonProperty("cid")]
        public string ContentId
        {
            get { return _contentId; }
        }

        [JsonProperty("dirty")]
        public bool Dirty
        {
            get { return _dirty; }
        }

        [JsonProperty("name")]
        public abstract string Name { get; }

        [JsonProperty("tooltip")]
        public abstract string Tooltip { get; }

        [JsonProperty("content")]
        public abstract string ContentString { get; }

        public abstract Content WithText(string newText);
    }
}
