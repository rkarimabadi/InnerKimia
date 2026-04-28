using System.Text.Json;
using System.Text.Json.Serialization;
using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Infrastructure.JsonConverters
{
    public class PositionValueConverter : JsonConverter<Position?>
    {
        public override Position? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();
            
            int row = 0, column = 0;
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Position(row, column);
                
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    
                    switch (propertyName?.ToLower())
                    {
                        case "row":
                            row = reader.GetInt32();
                            break;
                        case "column":
                        case "col":
                            column = reader.GetInt32();
                            break;
                    }
                }
            }
            
            throw new JsonException();
        }
        
        public override void Write(Utf8JsonWriter writer, Position? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            
            writer.WriteStartObject();
            writer.WriteNumber("row", value.Row);
            writer.WriteNumber("column", value.Column);
            writer.WriteEndObject();
        }
    }
}