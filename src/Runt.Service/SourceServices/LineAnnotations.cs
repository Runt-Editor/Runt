using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Service.SourceServices
{
    public class LineAnnotations
    {
        readonly ImmutableList<AnnotationRange> _ranges;

        public LineAnnotations(ImmutableList<AnnotationRange> ranges)
        {
            _ranges = ranges;
        }

        [JsonProperty("ranges")]
        public ImmutableList<AnnotationRange> Ranges
        {
            get { return _ranges; }
        }
    }

    public class AnnotationRange
    {
        readonly int _start;
        readonly int _end;
        readonly OrionStyle _style;

        public AnnotationRange(int start, int end, OrionStyle style)
        {
            _start = start;
            _end = end;
            _style = style;
        }

        [JsonProperty("start")]
        public int Start
        {
            get { return _start; }
        }

        [JsonProperty("end")]
        public int End
        {
            get { return _end; }
        }

        [JsonProperty("style")]
        public OrionStyle Style
        {
            get { return _style; }
        }
    }

    public class OrionStyle
    {
        readonly string _styleClass;
        readonly string _tagName;
        readonly JObject _style;
        readonly JObject _attributes;

        public OrionStyle(string styleClass = null, string tagName = null, JObject style = null, JObject attributes = null)
        {
            _style = style;
            _styleClass = styleClass;
            _tagName = tagName;
            _attributes = attributes;
        }

        [JsonProperty("attributes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JObject Attributes
        {
            get { return _attributes; }
        }

        [JsonProperty("tagName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TagName
        {
            get { return _tagName; }
        }

        [JsonProperty("style", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JObject Style
        {
            get { return _style; }
        }

        [JsonProperty("styleClass", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string StyleClass
        {
            get { return _styleClass; }
        }
    }
}
