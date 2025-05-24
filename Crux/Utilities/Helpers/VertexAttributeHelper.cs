using Crux.Graphics;

namespace Crux.Utilities.Helpers;

public static class VertexAttributeHelper
{
    public static int GetTypeWidth(Type attributeType)
    {
        if (attributeType == typeof(float))
            return 1;
        else if (attributeType == typeof(Vector2))
            return 2;
        else if (attributeType == typeof(Vector3))
            return 3;
        else if (attributeType == typeof(Vector4))
            return 4;
        else if (attributeType == typeof(Matrix4))
            return 16;
        else
            throw new ArgumentException($"Unsupported type {attributeType.Name} in dynamic VBO.");
    }

    public static int GetTypeByteSize(Type attributeType)
    {
        return GetTypeWidth(attributeType) * sizeof(float);
    }

    public static VertexAttribute ConvertToAttribute<T>(int layoutLocation, string attributeName, T[] vectors) where T : struct
    {
        if (typeof(T) == typeof(Vector4))
        {
            return new VertexAttribute(layoutLocation, attributeName, GetTypeWidth(typeof(T)), vectors.Cast<Vector4>().SelectMany(v => new float[] { v.X, v.Y, v.Z, v.W }).ToArray());
        }
        else if (typeof(T) == typeof(Vector3))
        {
            return new VertexAttribute(layoutLocation, attributeName, GetTypeWidth(typeof(T)), vectors.Cast<Vector3>().SelectMany(v => new float[] { v.X, v.Y, v.Z }).ToArray());
        }
        else if (typeof(T) == typeof(Vector2))
        {
            return new VertexAttribute(layoutLocation, attributeName, GetTypeWidth(typeof(T)), vectors.Cast<Vector2>().SelectMany(v => new float[] { v.X, v.Y }).ToArray());
        }
        else if (typeof(T) == typeof(float))
        {
            return new VertexAttribute(layoutLocation, attributeName, GetTypeWidth(typeof(T)), vectors.Cast<float>().ToArray());
        }
        else
        {
            throw new ArgumentException("Unsupported vertex attribute type.");
        }
    }
}
