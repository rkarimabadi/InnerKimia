using System.Text.Json;
using System.Text.Json.Serialization;
using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Infrastructure.JsonConverters
{
    public class ElementTypeJsonConverter : JsonConverter<ElementType>
    {
        public override ElementType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return Enum.TryParse<ElementType>(value, true, out var result) ? result : ElementType.Water;
        }
        
        public override void Write(Utf8JsonWriter writer, ElementType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}