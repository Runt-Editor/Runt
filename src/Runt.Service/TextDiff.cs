using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Runt.Service
{
    public class TextDiff
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("removed")]
        public int Removed { get; set; }

        [JsonProperty("added")]
        public int Added { get; set; }

        [JsonProperty("update")]
        public int Update { get; set; }
    }
}
