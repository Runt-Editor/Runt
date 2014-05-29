using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Runt.Core.Model;

namespace Runt.Service
{
    static class Messages
    {
        static readonly JsonSerializer _default = new JsonSerializer();

        public static string State(EditorState state, JsonSerializer serializer = null)
        {
            serializer = serializer ?? _default;
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
                serializer.Serialize(writer, state, typeof(EditorState));
            return sb.ToString();
        }
    }
}
