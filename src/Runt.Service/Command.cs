using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.Service
{
    public class Command
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("args")]
        public JArray Arguments { get; set; }
    }
}
