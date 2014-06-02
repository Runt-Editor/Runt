using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Runt.Core
{
    public static class JsonSerializerExtensions
    {
        public static void WriteProperty(this JsonSerializer serializer, JsonWriter writer, string propertyName, object value)
        {
            writer.WritePropertyName(propertyName);
            serializer.Serialize(writer, value);
        }
    }
}
