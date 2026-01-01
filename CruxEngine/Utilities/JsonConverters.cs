using System.Text.Json;
using System.Text.Json.Serialization;
using CruxEngine.Utilities.Helpers;

namespace CruxEngine.Utilities;

public class Color4JsonConverter : JsonConverter<Color4>
{
    public override void Write(Utf8JsonWriter writer, Color4 value, JsonSerializerOptions options)
    {
        string hex = $"{(byte)(value.R * 255):X2}{(byte)(value.G * 255):X2}{(byte)(value.B * 255):X2}";
        string alphaHex = $"{(byte)(value.A * 255):X2}";
        writer.WriteStringValue($"{hex}-{alphaHex}");
    }

    public override Color4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string str = reader.GetString()!;
        
        var parts = str.Split('-');
        string hex = parts[0];
        string alphaHex = (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) ? parts[1] : "FF";

        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
        byte a = Convert.ToByte(alphaHex, 16);

        return new Color4(r / 255f, g / 255f, b / 255f, a / 255f);
    }


    /*
    public override void Write(Utf8JsonWriter writer, Color4 value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new[] { value.R, value.G, value.B, value.A }, options);
    }
    
    public override Color4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<float[]>(ref reader, options)!;
        return new Color4(values[0], values[1], values[2], values[3]);
    }
    */
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

