using System;
using Newtonsoft.Json;
using Runt.Core.Model.FileTree;

namespace Runt.Core
{
    public class EntryJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Entry));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if(!(value is Entry))
            {
                serializer.Serialize(writer, value);
                return;
            }

            Entry entry = (Entry)value;
            writer.WriteStartObject();
            writer.WritePropertyName(Utils.NameOf(() => entry.RelativePath));
            writer.WriteValue(entry.RelativePath);
            writer.WritePropertyName(Utils.NameOf(() => entry.IsOpen));
            writer.WriteValue(entry.IsOpen);
            writer.WritePropertyName(Utils.NameOf(() => entry.Name));
            writer.WriteValue(entry.Name);
            writer.WritePropertyName(Utils.NameOf(() => entry.Type));
            writer.WriteValue(entry.Type);
            writer.WritePropertyName(Utils.NameOf(() => entry.Children));
            if (entry.IsOpen)
            {
                serializer.Serialize(writer, entry.Children);
            }
            else
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
            writer.WritePropertyName("has-" + Utils.NameOf(() => entry.Children));
            writer.WriteValue(entry.Children.Count > 0);
            writer.WriteEndObject();
        }
    }
}
