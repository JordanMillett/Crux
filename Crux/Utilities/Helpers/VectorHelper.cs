using System.Globalization;

namespace Crux.Utilities.Helpers;

public static class VectorHelper
{        
    public static float[] Flatten(Vector3 input)
    {
        return new float[] { input.X, input.Y, input.Z };
    }

    public static Vector3 LineToVector3(string line)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4)
        {
            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
            float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
            return new Vector3(x, y, z);
        }

        return Vector3.Zero;
    }
    
    public static Vector2 LineToVector2(string line)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
            return new Vector2(x, y);
        }

        return Vector2.Zero;
    }

    public static Vector3 Vector3Min(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Min(a.Z, b.Z)
        );
    }

    public static Vector3 Vector3Max(Vector3 a, Vector3 b)
    {
        return new Vector3(
            Math.Max(a.X, b.X),
            Math.Max(a.Y, b.Y),
            Math.Max(a.Z, b.Z)
        );
    }

    public static bool IsVectorNaN(Vector3 vector)
    {
        return float.IsNaN(vector.X) || float.IsNaN(vector.Y) || float.IsNaN(vector.Z);
    }

    public static float LengthSquared(Vector3 vector)
    {
        return vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
    }
}

