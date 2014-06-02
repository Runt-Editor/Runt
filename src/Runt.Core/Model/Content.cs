using Newtonsoft.Json;

namespace Runt.Core.Model
{
    public abstract class Content
    {
        readonly string _contentId;

        public Content(string contentId)
        {
            _contentId = contentId;
        }

        [JsonProperty("cid")]
        public string ContentId
        {
            get { return _contentId; }
        }

        [JsonProperty("name")]
        public abstract string Name { get; }

        [JsonProperty("tooltip")]
        public abstract string Tooltip { get; }

        [JsonProperty("content")]
        public abstract string ContentString { get; }
    }
}
