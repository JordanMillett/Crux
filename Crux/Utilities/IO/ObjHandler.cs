using Crux.Graphics;
using Crux.Utilities.Helpers;

namespace Crux.Utilities.IO;

public static class ObjHandler
{
    public static Dictionary<string, (Mesh mesh, int users)> UniqueMeshes = new Dictionary<string, (Mesh mesh, int users)>();

    public static Mesh LoadObjAsMesh(ref string path)
    {
        if (!AssetHandler.AssetExists(path))
        {
            Logger.LogWarning($"Mesh {path} not found");
            //path = fallbackMeshPath;
        }

        if (UniqueMeshes.TryGetValue(path, out var cached))
        {
            cached.users++;
            UniqueMeshes[path] = cached;
            return cached.mesh;
        }else
        {
            Mesh loaded = LoadAsMesh(ref path);
            UniqueMeshes.Add(path, (loaded, 1));
            return loaded;
        }
    }

    public static Mesh LoadAsMesh(ref string path)
    {
        List<Vertex> fullVertices = new List<Vertex>();
        List<uint> fullIndices = new List<uint>();

        Dictionary<string, List<Vertex>> submeshVertices = new();
        Dictionary<string, List<uint>> submeshIndices = new();

        string currentGroup = "default";

        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        using (StreamReader reader = new StreamReader(AssetHandler.GetStream(path)))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("v ")) // Vertex position
                {
                    positions.Add(VectorHelper.LineToVector3(line));
                }else if(line.StartsWith("vn "))
                {
                    normals.Add(VectorHelper.LineToVector3(line));
                }else if(line.StartsWith("vt "))
                {
                    uvs.Add(VectorHelper.LineToVector2(line));
                }
                else if (line.StartsWith("g ")) // Material group
                {
                    currentGroup = line.Substring(2).Trim();

                    if (!submeshIndices.ContainsKey(currentGroup))
                    {
                        submeshIndices[currentGroup] = new List<uint>();
                        submeshVertices[currentGroup] = new List<Vertex>();
                    }
                }
                else if (line.StartsWith("f ")) // Face indices
                {
                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string[] vertexData = parts[i].Split('/');
                        uint positionIndex = uint.Parse(vertexData[0]) - 1;
                        uint normalIndex = uint.Parse(vertexData[2]) - 1;
                        uint uvIndex = uint.Parse(vertexData[1]) - 1;
                    
                        // We must construct the vertex with position, normal, and UV based on these indices
                        Vector3 position = positions[(int)positionIndex];
                        Vector3 normal = normals[(int)normalIndex];
                        Vector2 uv = uvs[(int)uvIndex];

                        Vertex vertex = new Vertex(position, normal, uv);
                        if (!fullVertices.Contains(vertex))
                        {
                            fullVertices.Add(vertex);
                        }
                        uint fullIndex = (uint)fullVertices.IndexOf(vertex);
                        fullIndices.Add(fullIndex);

                        // Add to submesh
                        if (!submeshVertices[currentGroup].Contains(vertex))
                        {
                            submeshVertices[currentGroup].Add(vertex);
                        }
                        uint subIndex = (uint)submeshVertices[currentGroup].IndexOf(vertex);
                        submeshIndices[currentGroup].Add(subIndex);
                    }
                }
            }

            // Create the full mesh
            Mesh fullMesh = new Mesh(fullIndices.ToArray(), fullVertices.ToArray());

            // Create submeshes and add them to the full mesh
            foreach (var kvp in submeshIndices)
            {
                string groupName = kvp.Key;
                uint[] indicesArray = kvp.Value.ToArray();
                Vertex[] verticesArray = submeshVertices[groupName].ToArray();

                Mesh submesh = new Mesh(indicesArray, verticesArray);
                fullMesh.Submeshes.Add(submesh);
            }

            return fullMesh;
        }
    }
}
