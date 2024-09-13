using Crux.Utilities.Helpers;

namespace Crux.Graphics;

public class Mesh
{
    public uint[] Indices { get; }
    public Vertex[] Vertices { get; }
    public Vector3 MinBounds { get; set; }
    public Vector3 MaxBounds { get; set; }

    public List<Mesh> Submeshes { get; set; } = new List<Mesh>();

    public Vector3 OffsetFromCenter = Vector3.Zero;

    public Mesh(uint[] indices, Vertex[] vertices)
    {
        Indices = indices;
        Vertices = vertices;
        CalculateBoundsAndOffsets();
    }
    
    public void CalculateBoundsAndOffsets()
    {
        if (Vertices.Length == 0)
        {
            MinBounds = new Vector3(float.MaxValue);
            MaxBounds = new Vector3(float.MinValue);
            return;
        }

        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);

        foreach (var vertex in Vertices)
        {
            min = VectorHelper.Vector3Min(min, vertex.Position);
            max = VectorHelper.Vector3Max(max, vertex.Position);
        }

        MinBounds = min;
        MaxBounds = max;
    }
    
    public Mesh Clone()
    {
        uint[] clonedIndices = (uint[])Indices.Clone();
        Vertex[] clonedVertices = new Vertex[Vertices.Length];

        for (int i = 0; i < Vertices.Length; i++)
        {
            clonedVertices[i] = Vertices[i].Clone();
        }

        Mesh cloned = new Mesh(clonedIndices, clonedVertices);

        foreach (Mesh submesh in Submeshes)
        {
            cloned.Submeshes.Add(submesh.Clone());
        }

        return cloned;
    }
    
    public float[] GetInterleavedVertexData()
    {
        List<float> interleavedData = new List<float>();

        foreach (Vertex vertex in Vertices)
        {
            // Add position (X, Y, Z)
            interleavedData.Add(vertex.Position.X);
            interleavedData.Add(vertex.Position.Y);
            interleavedData.Add(vertex.Position.Z);

            // Add normal (X, Y, Z)
            interleavedData.Add(vertex.Normal.X);
            interleavedData.Add(vertex.Normal.Y);
            interleavedData.Add(vertex.Normal.Z);

            // Add UV (X, Y)
            interleavedData.Add(vertex.UV.X);
            interleavedData.Add(vertex.UV.Y);
        }

        return interleavedData.ToArray();
    }

    public (Vector3 center, Vector3[] axes, Vector3 halfExtents) GetWorldSpaceOBB(Matrix4 modelMatrix)
    {
        MatrixHelper.Decompose(modelMatrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation);
        
        Vector3 localCenter = (MinBounds + MaxBounds) / 2.0f;
        Vector3 halfExtents = (MaxBounds - MinBounds) / 2.0f;

        Vector3[] localAxes = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1)
        };
    
        Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(rotation);
        Vector3[] worldAxes = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            worldAxes[i] = Vector3.Normalize(Vector3.TransformNormal(localAxes[i], rotationMatrix));
        }

        Vector3 worldHalfExtents = new Vector3(
            halfExtents.X * scale.X,
            halfExtents.Y * scale.Y,
            halfExtents.Z * scale.Z
        );

        Vector3 worldCenter = Vector3.TransformPosition(localCenter, modelMatrix);
        return (worldCenter, worldAxes, worldHalfExtents);
    }

    public (Vector3 min, Vector3 max) GetWorldSpaceAABB(Matrix4 modelMatrix)
    {
        Vector3[] corners =
        {
            new Vector3(MinBounds.X, MinBounds.Y, MinBounds.Z),
            new Vector3(MinBounds.X, MinBounds.Y, MaxBounds.Z),
            new Vector3(MinBounds.X, MaxBounds.Y, MinBounds.Z),
            new Vector3(MinBounds.X, MaxBounds.Y, MaxBounds.Z),
            new Vector3(MaxBounds.X, MinBounds.Y, MinBounds.Z),
            new Vector3(MaxBounds.X, MinBounds.Y, MaxBounds.Z),
            new Vector3(MaxBounds.X, MaxBounds.Y, MinBounds.Z),
            new Vector3(MaxBounds.X, MaxBounds.Y, MaxBounds.Z),
        };

        Vector3 transformedMin = new Vector3(float.MaxValue);
        Vector3 transformedMax = new Vector3(float.MinValue);

        foreach (var corner in corners)
        {
            Vector3 transformedCorner = Vector3.TransformPosition(corner, modelMatrix);
            transformedMin = Vector3.ComponentMin(transformedMin, transformedCorner);
            transformedMax = Vector3.ComponentMax(transformedMax, transformedCorner);
        }

        return (transformedMin, transformedMax);
    }

    public Vector3 GetRandomPositionOnMesh(Random random)
    {
        List<float> cumulativeAreas = new List<float>();
        float totalArea = 0f;

        for (int i = 0; i < Indices.Length; i += 3)
        {
            Vector3 v0 = Vertices[Indices[i]].Position;
            Vector3 v1 = Vertices[Indices[i + 1]].Position;
            Vector3 v2 = Vertices[Indices[i + 2]].Position;
            totalArea += Vector3.Cross(v1 - v0, v2 - v0).Length / 2.0f;
            cumulativeAreas.Add(totalArea);
        }

        float r = (float)random.NextDouble() * totalArea;
        int triangleIdx = cumulativeAreas.FindIndex(area => area >= r);
        if (triangleIdx == -1) triangleIdx = cumulativeAreas.Count - 1;
        int baseIdx = triangleIdx * 3;

        Vector3 v0_final = Vertices[Indices[baseIdx]].Position;
        Vector3 v1_final = Vertices[Indices[baseIdx + 1]].Position;
        Vector3 v2_final = Vertices[Indices[baseIdx + 2]].Position;

        float u = (float)random.NextDouble();
        float v = (float)random.NextDouble();

        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        float w = 1 - u - v;
        return (u * v0_final) + (v * v1_final) + (w * v2_final);
    }
}
