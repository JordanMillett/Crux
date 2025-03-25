using Crux.Graphics;

namespace Crux.Utilities.Helpers;

public static class VertexAttributeHelper
{
    public static int StrideLookup(Type attributeType)
    {
        if (attributeType == typeof(float))
        {
            return sizeof(float);
        }
        else if (attributeType == typeof(Vector2))
        {
            return 2 * sizeof(float);
        }
        else if (attributeType == typeof(Vector3))
        {
            return 3 * sizeof(float);
        }
        else if (attributeType == typeof(Vector4))
        {
            return 4 * sizeof(float);
        }
        else if (attributeType == typeof(Matrix4))
        {
            return 16 * sizeof(float);
        }
        else
        {
            throw new ArgumentException($"Unsupported type {attributeType.Name} in dynamic VBO.");
        }
    }

    public static VertexAttribute ConvertToAttribute<T>(string attributeName, T[] vectors) where T : struct
    {
        if (typeof(T) == typeof(Vector4))
        {
            return new VertexAttribute(attributeName, 4, vectors.Cast<Vector4>().SelectMany(v => new float[] { v.X, v.Y, v.Z, v.W }).ToArray());
        }
        else if (typeof(T) == typeof(Vector3))
        {
            return new VertexAttribute(attributeName, 3, vectors.Cast<Vector3>().SelectMany(v => new float[] { v.X, v.Y, v.Z }).ToArray());
        }
        else if (typeof(T) == typeof(Vector2))
        {
            return new VertexAttribute(attributeName, 2, vectors.Cast<Vector2>().SelectMany(v => new float[] { v.X, v.Y }).ToArray());
        }
        else if (typeof(T) == typeof(float))
        {
            return new VertexAttribute(attributeName, 1, vectors.Cast<float>().ToArray());
        }
        else
        {
            throw new ArgumentException("Unsupported vertex attribute type.");
        }
    }
}
