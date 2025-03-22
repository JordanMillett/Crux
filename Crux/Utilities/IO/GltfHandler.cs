using Crux.Graphics;
using System.Text.Json.Nodes;
using Crux.Components;

namespace Crux.Utilities.IO;

public static class GltfHandler
{
    private static GameObject ProcessObjectNodeIntoGameObjectWithMesh(string path, JsonNode rootNode, JsonNode objectNode, out List<string> textures)
    {
        string folder = Path.GetDirectoryName(path);

        Mesh fullMesh = ProcessObjectNodeToMesh(
            folder, rootNode, objectNode,
            out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale, out textures
        );

        GameObject Made = GameEngine.Link.InstantiateGameObject(objectNode["name"].GetValue<string>());
        Made.Transform.WorldPosition = objectPosition;
        Made.Transform.WorldRotation = objectRotation;
        Made.Transform.Scale = objectScale;
        Made.AddComponent<MeshComponent>().data = fullMesh;
        Made.GetComponent<MeshComponent>().path = path + "_" + objectNode["name"].GetValue<string>();

        return Made;
    }

    private static GameObject ProcessObjectNodeIntoEmptyGameObject(string path, JsonNode rootNode, JsonNode objectNode)
    {
        ProcessObjectNodeToTransform(objectNode, out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale);

        GameObject Made = GameEngine.Link.InstantiateGameObject(objectNode["name"].GetValue<string>()); 
        Made.Transform.WorldPosition = objectPosition;
        Made.Transform.WorldRotation = objectRotation;
        Made.Transform.Scale = objectScale;

        return Made;
    }

    public static Dictionary<string, GameObject>? LoadGltfAsMeshRenderers(string path)
    {
        if(!AssetHandler.AssetExists(path))
            return null;

        Dictionary<string, GameObject> All = new();
        using (StreamReader reader = new StreamReader(AssetHandler.GetStream(path)))
        {
            string json = reader.ReadToEnd();
            JsonNode rootNode = JsonNode.Parse(json);
            
            foreach (int objectIndex in rootNode["scenes"][0]["nodes"].AsArray())
            {
                JsonNode objectNode = rootNode["nodes"][objectIndex];

                GameObject Made = ProcessObjectNodeIntoGameObjectWithMesh(path, rootNode, objectNode, out List<string> textures);
                All.Add(objectNode["name"].GetValue<string>(), Made);

                List<Shader> Mats = new List<Shader>();
                for(int i = 0; i < textures.Count; i++)
                {    
                    textures[i] = $"{AssetHandler.GameAssetPath}/Textures/" + textures[i];
                    if(AssetHandler.AssetExists(textures[i] + ".jpg"))
                        textures[i] = textures[i] + ".jpg";
                    else if(AssetHandler.AssetExists(textures[i] + ".png"))
                        textures[i] = textures[i] + ".png";
                        
                    
                    Mats.Add(AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Lit, textures[i]));
                }
                Made.AddComponent<MeshRenderComponent>().SetShaders(Mats);
            }
        }

        return All;
    }

    public static GameObject? LoadGltfAsMeshRenderer(string path)
    {
        if(!AssetHandler.AssetExists(path))
            return null;

        using (StreamReader reader = new StreamReader(AssetHandler.GetStream(path)))
        {
            string json = reader.ReadToEnd();
            JsonNode rootNode = JsonNode.Parse(json);
            
            JsonNode objectNode = rootNode["nodes"][0];
            GameObject Made = ProcessObjectNodeIntoGameObjectWithMesh(path, rootNode, objectNode, out List<string> textures);

            List<Shader> Mats = new List<Shader>();
            for(int i = 0; i < textures.Count; i++)
            {    
                textures[i] = $"{AssetHandler.GameAssetPath}/Textures/" + textures[i];
                if(AssetHandler.AssetExists(textures[i] + ".jpg"))
                    textures[i] = textures[i] + ".jpg";
                else if(AssetHandler.AssetExists(textures[i] + ".png"))
                    textures[i] = textures[i] + ".png";

                Mats.Add(AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Lit, textures[i]));
            }
            Made.AddComponent<MeshRenderComponent>().SetShaders(Mats);

            return Made;
        }
    }

    public static Dictionary<string, GameObject>? LoadGltfAsEmpties(string path)
    {
        if(!AssetHandler.AssetExists(path))
            return null;

        Dictionary<string, GameObject> All = new();

        using (StreamReader reader = new StreamReader(AssetHandler.GetStream(path)))
        {
            string json = reader.ReadToEnd();
            JsonNode rootNode = JsonNode.Parse(json);
            
            foreach (int objectIndex in rootNode["scenes"][0]["nodes"].AsArray())
            {
                JsonNode objectNode = rootNode["nodes"][objectIndex];

                GameObject Made = ProcessObjectNodeIntoEmptyGameObject(path, rootNode, objectNode);
                All.Add(objectNode["name"].GetValue<string>(), Made);
            }
        }
        return All;
    }

    public static GameObject? LoadGltfAsEmpty(string path)
    {
        if(!AssetHandler.AssetExists(path))
            return null;

        using (StreamReader reader = new StreamReader(AssetHandler.GetStream(path)))
        {
            string json = reader.ReadToEnd();
            JsonNode rootNode = JsonNode.Parse(json);
            
            JsonNode objectNode = rootNode["nodes"][0];
            GameObject Made = ProcessObjectNodeIntoEmptyGameObject(path, rootNode, objectNode);
            return Made;
        }
    }

    public static Mesh? LoadGltfAsMesh(ref string path, out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale)
    {
        objectPosition = Vector3.Zero;
        objectRotation = Quaternion.Identity;
        objectScale = Vector3.One;

        if(!AssetHandler.AssetExists(path))
            return null;

        using (StreamReader reader = new StreamReader(AssetHandler.GetStream(path)))
        {
            string json = reader.ReadToEnd();
            JsonNode rootNode = JsonNode.Parse(json);
            string folder = Path.GetDirectoryName(path);

            JsonNode objectNode = rootNode["nodes"][0];

            Mesh fullMesh = ProcessObjectNodeToMesh(
                    folder, rootNode, objectNode,
                    out objectPosition, out objectRotation, out objectScale, out _
                );
            
            return fullMesh;
        }
    }

    public static Mesh? LoadGltfAsMeshWithMaterials(string path, out List<Shader> materials)
    {
        materials = [];
        if(!AssetHandler.AssetExists(path))
            return null;

        using (StreamReader reader = new StreamReader(AssetHandler.GetStream(path)))
        {
            string json = reader.ReadToEnd();
            JsonNode rootNode = JsonNode.Parse(json);
            string folder = Path.GetDirectoryName(path);

            JsonNode objectNode = rootNode["nodes"][0];

            Mesh fullMesh = ProcessObjectNodeToMesh(
                    folder, rootNode, objectNode,
                    out _, out _, out _, out List<string> textures
                );
            
            for(int i = 0; i < textures.Count; i++)
            {    
                textures[i] = $"{AssetHandler.GameAssetPath}/Textures/" + textures[i];
                if(AssetHandler.AssetExists(textures[i] + ".jpg"))
                    textures[i] = textures[i] + ".jpg";
                else if(AssetHandler.AssetExists(textures[i] + ".png"))
                    textures[i] = textures[i] + ".png";

                
                materials.Add(AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Lit, textures[i]));
            }

            return fullMesh;
        }
    }

    private static Mesh ProcessObjectNodeToMesh(
        string folder, JsonNode rootNode, JsonNode objectNode, 
        out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale, out List<string> textures
    )
    {
        ProcessObjectNodeToTransform(objectNode, out objectPosition, out objectRotation, out objectScale);

        textures = [];
        int meshIndex = objectNode["mesh"].GetValue<int>();
        JsonNode meshNode = rootNode["meshes"][meshIndex];
        JsonNode materialsNode = rootNode["materials"];

        List<Mesh> submeshes = new List<Mesh>();
        List<Vertex> fullVertices = new List<Vertex>();
        List<uint> fullIndices = new List<uint>();
        foreach (JsonNode primitiveNode in meshNode["primitives"].AsArray())
        {
            int indexAccessor = primitiveNode["indices"].GetValue<int>();
            int positionAccessor = primitiveNode["attributes"]["POSITION"].GetValue<int>();
            int normalAccessor = primitiveNode["attributes"]["NORMAL"].GetValue<int>();
            int uvAccessor = primitiveNode["attributes"]["TEXCOORD_0"].GetValue<int>();
            
            List<uint> indices = GetBufferData<uint>(folder, rootNode, indexAccessor);
            List<Vector3> positions = GetBufferData<Vector3>(folder, rootNode, positionAccessor);
            List<Vector3> normals = GetBufferData<Vector3>(folder, rootNode, normalAccessor);
            List<Vector2> uvs = GetBufferData<Vector2>(folder, rootNode, uvAccessor);

            List<Vertex> subVertices = new List<Vertex>();
            uint offset = (uint) fullVertices.Count;

            for (int i = 0; i < positions.Count; i++)
            {
                Vertex vertex = new Vertex(positions[i], normals[i], uvs[i]);
                subVertices.Add(vertex);
                fullVertices.Add(vertex);
            }   

            foreach (uint index in indices)
            {
                fullIndices.Add(index + offset);
            }

            submeshes.Add(new Mesh(indices.ToArray(), subVertices.ToArray()));
            
            if(primitiveNode is JsonObject primitive)
            {
                if(primitive.ContainsKey("material"))
                {
                    int materialIndex = primitiveNode["material"].GetValue<int>();
                    textures.Add(materialsNode[materialIndex]["name"].GetValue<string>());
                }
            }
        }

        Mesh fullMesh = new Mesh(fullIndices.ToArray(), fullVertices.ToArray());
        fullMesh.Submeshes.AddRange(submeshes);
        return fullMesh;
    }

    private static void ProcessObjectNodeToTransform(JsonNode objectNode, out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale)
    {
        objectPosition = Vector3.Zero;
        objectRotation = Quaternion.Identity;
        objectScale = Vector3.One;

        if(objectNode is JsonObject node)
        {
            if(node.ContainsKey("translation"))
            {
                objectPosition = new Vector3
                (
                    objectNode["translation"][0].GetValue<float>(), 
                    objectNode["translation"][1].GetValue<float>(), 
                    objectNode["translation"][2].GetValue<float>()
                );
            }

            if(node.ContainsKey("rotation"))
            {
                objectRotation = new Quaternion
                (
                    objectNode["rotation"][0].GetValue<float>(), 
                    objectNode["rotation"][1].GetValue<float>(), 
                    objectNode["rotation"][2].GetValue<float>(), 
                    objectNode["rotation"][3].GetValue<float>()
                );
            }

            if(node.ContainsKey("scale"))
            {
                objectScale = new Vector3
                (
                    objectNode["scale"][0].GetValue<float>(), 
                    objectNode["scale"][1].GetValue<float>(), 
                    objectNode["scale"][2].GetValue<float>()
                );
            }
        }
    }

    public static List<T> GetBufferData<T>(string directory, JsonNode rootNode, int accessorIndex) where T : struct
    {
        JsonNode accessorNode = rootNode["accessors"][accessorIndex];
        int bufferViewIndex = accessorNode["bufferView"].GetValue<int>();
        JsonNode bufferViewNode = rootNode["bufferViews"][bufferViewIndex];
        int bufferIndex = bufferViewNode["buffer"].GetValue<int>();
        string bufferPath = Path.Combine(directory, rootNode["buffers"][bufferIndex]["uri"].GetValue<string>());
        
        byte[] bufferData;
        using (Stream stream = AssetHandler.GetStream(bufferPath))
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bufferData = memoryStream.ToArray();

                int byteOffset = bufferViewNode["byteOffset"]?.GetValue<int>() ?? 0;
                byteOffset += accessorNode["byteOffset"]?.GetValue<int>() ?? 0;
                
                int count = accessorNode["count"].GetValue<int>();
                int componentType = accessorNode["componentType"].GetValue<int>(); 
                string accessorType = accessorNode["type"].GetValue<string>(); // VEC2, VEC3, etc.

                List<T> result = new List<T>();
                int stride = bufferViewNode["byteStride"]?.GetValue<int>() ?? GetDefaultStride(accessorType, componentType);
                
                for (int i = 0; i < count; i++)
                {
                    int elementOffset = byteOffset + i * stride;

                    if (typeof(T) == typeof(Vector3) && accessorType == "VEC3")
                    {
                        float x = BitConverter.ToSingle(bufferData, elementOffset);
                        float y = BitConverter.ToSingle(bufferData, elementOffset + 4);
                        float z = BitConverter.ToSingle(bufferData, elementOffset + 8);
                        result.Add((T)(object)new Vector3(x, y, z));
                    }
                    else if (typeof(T) == typeof(Vector2) && accessorType == "VEC2")
                    {
                        float u = BitConverter.ToSingle(bufferData, elementOffset);
                        float v = BitConverter.ToSingle(bufferData, elementOffset + 4);
                        result.Add((T)(object)new Vector2(u, 1.0f - v)); // Flip V for GLTF
                    }
                    else if (typeof(T) == typeof(uint) && accessorType == "SCALAR")
                    {
                        uint index = componentType switch
                        {
                            5125 => BitConverter.ToUInt32(bufferData, elementOffset), // GL_UNSIGNED_INT (4 bytes)
                            5123 => BitConverter.ToUInt16(bufferData, elementOffset), // GL_UNSIGNED_SHORT (2 bytes)
                            5121 => bufferData[elementOffset], // GL_UNSIGNED_BYTE (1 byte)
                        };
                        result.Add((T)(object)index);
                    }
                }
                return result;
            }
        }
    }

    private static int GetDefaultStride(string accessorType, int componentType)
    {
        int componentSize = componentType switch
        {
            5126 => 4, // FLOAT (4 bytes)
            5125 => 4, // UNSIGNED_INT (4 bytes)
            5123 => 2, // UNSIGNED_SHORT (2 bytes)
            5121 => 1, // UNSIGNED_BYTE (1 byte)
            _ => throw new Exception($"Unknown component type: {componentType}")
        };

        int numComponents = accessorType switch
        {
            "SCALAR" => 1,
            "VEC2" => 2,
            "VEC3" => 3,
            "VEC4" => 4,
            "MAT2" => 4,  // 2x2 matrix
            "MAT3" => 9,  // 3x3 matrix
            "MAT4" => 16, // 4x4 matrix
            _ => throw new Exception($"Unknown accessor type: {accessorType}")
        };

        return componentSize * numComponents;
    }
}
