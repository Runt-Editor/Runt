using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Runt.DesignTimeHost
{
    public class Message
    {
        public string HostId { get; set; }
        public string MessageType { get; set; }
        public int ContextId { get; set; }
        public JToken Payload { get; set; }

        public override string ToString()
        {
            return "(" + HostId + ", " + MessageType + ", " + ContextId + ") -> " + (Payload == null ? "null" : Payload.ToString(Formatting.Indented));
        }
    }
}
