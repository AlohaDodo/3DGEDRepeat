using Microsoft.Xna.Framework;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GDEngine.Core.Serialization
{
    /// <summary>
    /// JSON converter for Microsoft.Xna.Framework.Vector3.
    /// Accepts either array form [x,y,z] or object form {"x":X,"y":Y,"z":Z}.
    /// Writes as [x,y,z].
    /// </summary>
    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read(); float x = (float)reader.GetDouble();
                reader.Read(); float y = (float)reader.GetDouble();
                reader.Read(); float z = (float)reader.GetDouble();
                reader.Read(); // EndArray
                return new Vector3(x, y, z);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                float x = 0, y = 0, z = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string? name = reader.GetString();
                    reader.Read();
                    float val = (float)reader.GetDouble();
                    if (string.Equals(name, "x", StringComparison.OrdinalIgnoreCase)) x = val;
                    else if (string.Equals(name, "y", StringComparison.OrdinalIgnoreCase)) y = val;
                    else if (string.Equals(name, "z", StringComparison.OrdinalIgnoreCase)) z = val;
                }
                return new Vector3(x, y, z);
            }

            throw new JsonException("Vector3 must be [x,y,z] or {x,y,z}.");
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.Z);
            writer.WriteEndArray();
        }
    }
}
