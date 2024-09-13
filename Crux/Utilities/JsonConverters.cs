using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crux.Utilities;

public class Color4JsonConverter : JsonConverter<Color4>
{
    public override void Write(Utf8JsonWriter writer, Color4 value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new[] { value.R, value.G, value.B, value.A }, options);
    }
    
    public override Color4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options)!;
        return new Color4(values[0], values[1], values[2], values[3]);
    }
}

public class QuaternionJsonConverter : JsonConverter<Quaternion>
{
    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new[] { value.X, value.Y, value.Z, value.W }, options);
    }
    
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options)!;
        return new Quaternion(values[0], values[1], values[2], values[3]);
    }
}

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new[] { value.X, value.Y, value.Z }, options);
    }
    
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options)!;
        return new Vector3(values[0], values[1], values[2]);
    }
}

public class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new[] { value.X, value.Y }, options);
    }
    
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options)!;
        return new Vector2(values[0], values[1]);
    }
}

