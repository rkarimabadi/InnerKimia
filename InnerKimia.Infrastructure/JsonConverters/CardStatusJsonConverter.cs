using System.Text.Json;
using System.Text.Json.Serialization;
using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Infrastructure.JsonConverters
{
    public class CardStatusJsonConverter : JsonConverter<CardStatus>
    {
        public override CardStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return Enum.TryParse<CardStatus>(value, true, out var result) ? result : CardStatus.InDeck;
        }
        
        public override void Write(Utf8JsonWriter writer, CardStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}