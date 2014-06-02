using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Core.Model
{
    public class Tab
    {
        readonly string _name;
        readonly string _tooltip;
        readonly bool _dirty;
        readonly bool _active;
        readonly string _contentId;

        public Tab(string contentId, string name, string tooltip, bool dirty, bool active)
        {
            _name = name;
            _tooltip = tooltip;
            _dirty = dirty;
            _active = active;
            _contentId = contentId;
        }

        public Tab AsActive(bool active, JObject change)
        {
            if (active == _active)
                return this;

            Utils.RegisterChange(change, () => Active, active, null);
            return new Tab(_contentId, _name, _tooltip, _dirty, active);
        }

        public Tab AsDirty(bool dirty, JObject change)
        {
            if (dirty == _dirty)
                return this;

            Utils.RegisterChange(change, () => Dirty, dirty, null);
            return new Tab(_contentId, _name, _tooltip, dirty, _active);
        }

        [JsonProperty("name")]
        public string Name
        {
            get { return _name; }
        }

        [JsonProperty("dirty")]
        public bool Dirty
        {
            get { return _dirty; }
        }

        [JsonProperty("active")]
        public bool Active
        {
            get { return _active; }
        }

        [JsonProperty("tooltip")]
        public string Tooltip
        {
            get { return _tooltip; }
        }

        [JsonProperty("cid")]
        public string ContentId
        {
            get { return _contentId; }
        }
    }
}
