using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using Crux.Utilities.IO;
using Crux.Physics;

namespace Crux.Graphics;

public static class GraphicsCache
{
    static Dictionary<string, (int id, int users)> Textures = new();
    
    static Dictionary<string, (int id, int users)> Vertex = new();
    static Dictionary<string, (int id, int users)> Fragment = new();
    static Dictionary<(int vertId, int fragId), (int id, int users)> Programs = new();
    
    static Dictionary<string, (int vao, int users)> VAOs = new();
    static Dictionary<string, (int vao, int vbo, int users)> InstanceVAOs = new();

    public static int DrawCallsThisFrame = 0;
    public static int MeshDrawCallsThisFrame = 0;
    public static float FramesPerSecond = 0f;

    public static Octree Tree;

    static GraphicsCache()
    {
        Tree = new Octree(new Vector3(-500, -500, -500), new Vector3(500, 500, 500), 5, "Visibility Octree");
    }
    
    public static string GetShortInfo()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"FPS - {FramesPerSecond:F2}");
        sb.AppendLine($"Mesh Draw Calls - {MeshDrawCallsThisFrame}");
        sb.AppendLine($"Draw Calls - {DrawCallsThisFrame}");

        sb.AppendLine($"Standard VAOs - {VAOs.Count}x");
        sb.AppendLine($"Instance VAOs - {InstanceVAOs.Count}x");
        sb.AppendLine($"Unique Textures - {Textures.Count}x");
        sb.AppendLine($"Unique Shader Programs - {Programs.Count}x");

        return sb.ToString();
    }

    public static string GetFullInfo()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"FPS - {FramesPerSecond:F2}");
        sb.AppendLine($"Mesh Draw Calls - {MeshDrawCallsThisFrame}");
        sb.AppendLine($"Draw Calls - {DrawCallsThisFrame}");

        sb.AppendLine($"Standard VAOs - {VAOs.Count}x");
        foreach (var entry in VAOs)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Instance VAOs - {InstanceVAOs.Count}x");
        foreach (var entry in InstanceVAOs)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Textures - {Textures.Count}x");
        foreach (var entry in Textures)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Vertex Shaders - {Vertex.Count}x");
        foreach (var entry in Vertex)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Fragment Shaders - {Fragment.Count}x");
        foreach (var entry in Fragment)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Shader Programs - {Programs.Count}x");
        int totalProgramUsers = 0;
        foreach (var entry in Programs)
        {
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");
            totalProgramUsers += entry.Value.users;
        }
        sb.AppendLine($"Shader Program Users - {totalProgramUsers}x");

        return sb.ToString();
    }

    public static int GetTexture(string path)
    {
        if (Textures.TryGetValue(path, out var cached))
        {
            cached.users++;
            Textures[path] = cached;
            return cached.id;
        }
        else
        {
            StbImage.stbi_set_flip_vertically_on_load(1);
            using (var stream = AssetHandler.GetStream(path))
            {
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);


                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                                PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest);
                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);



                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                GL.BindTexture(TextureTarget.Texture2D, 0);

                Textures.Add(path, (id, 1));

                return id;
            }
        }
    }
    
    public static void RemoveTextureUser(string path)
    {
        if (Textures.TryGetValue(path, out var cached))
        {
            cached.users--;
            
            if(cached.users == 0)
            {
                GL.DeleteTexture(cached.id);
                Textures.Remove(path);
            }else
            {
                Textures[path] = cached; 
            }
        }
    }
    
    public static int GetVertexShader(string path)
    {
        if (Vertex.TryGetValue(path, out var cached))
        {
            cached.users++;
            Vertex[path] = cached;
            return cached.id;
        }else
        {
            int id = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(id, AssetHandler.ReadAssetInFull(path));
            GL.CompileShader(id);
            string vertexShaderLog = GL.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(vertexShaderLog))
            {
                //GL.DeleteShader(id);
                throw new Exception($"Error compiling vertex shader {path}: {vertexShaderLog}");
            }

            Vertex.Add(path, (id, 1));

            return id;
        }
    }
    
    public static void RemoveVertexUser(string path)
    {
        if (Vertex.TryGetValue(path, out var cached))
        {
            cached.users--;
            
            if(cached.users == 0)
            {
                Vertex.Remove(path);
                GL.DeleteShader(cached.id);
            }else
            {
                Vertex[path] = cached; 
            }
        }
    }
    
    public static int GetFragmentShader(string path)
    {
        if (Fragment.TryGetValue(path, out var cached))
        {
            cached.users++;
            Fragment[path] = cached;
            return cached.id;
        }else
        {
            int id = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(id, AssetHandler.ReadAssetInFull(path));
            GL.CompileShader(id);
            string fragmentShaderLog = GL.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(fragmentShaderLog))
            {
                //GL.DeleteShader(id);
                throw new Exception($"Error compiling fragment shader {path}: {fragmentShaderLog}");
            }

            Fragment.Add(path, (id, 1));

            return id;
        }
    }
    
    public static void RemoveFragmentUser(string path)
    {
        if (Fragment.TryGetValue(path, out var cached))
        {
            cached.users--;
            
            if(cached.users == 0)
            {
                Fragment.Remove(path);
                GL.DeleteShader(cached.id);
            }else
            {
                Fragment[path] = cached; 
            }
        }
    }
    
    public static int GetProgram((int vert, int frag) ids)
    {   
        if (Programs.TryGetValue(ids, out var cached))
        {
            cached.users++;
            Programs[ids] = cached;
            return cached.id;
        }else
        {
            int id = GL.CreateProgram();
            GL.AttachShader(id, ids.vert);
            GL.AttachShader(id, ids.frag);

            GL.LinkProgram(id);
            string programLog = GL.GetProgramInfoLog(id);
            if (!string.IsNullOrEmpty(programLog))
            {
                //GL.DeleteProgram(id);
                throw new Exception($"Error linking program: {programLog}");
            }

            GL.ValidateProgram(id);
            string validationLog = GL.GetProgramInfoLog(id);
            if (!string.IsNullOrEmpty(validationLog))
            {
                //GL.DeleteProgram(id);
                throw new Exception($"Error validating program: {validationLog}");
            }
            
            GL.DetachShader(id, ids.vert);
            GL.DetachShader(id, ids.frag);

            Programs.Add(ids, (id, 1));

            return id;
        }
    }
    
    public static void RemoveProgramUser((int vert, int frag) ids)
    {
        if (Programs.TryGetValue(ids, out var cached))
        {
            cached.users--;
            
            if(cached.users == 0)
            {
                GL.DeleteProgram(cached.id);
                Programs.Remove(ids);
            }else
            {
                Programs[ids] = cached; 
            }
        }
    }

    public static (int vao, int vbo) GetInstancedUIVAO()
    {
        string path = "ui";
        
        if (InstanceVAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            InstanceVAOs[path] = cached;
            return (cached.vao, cached.vbo);
        }
        else
        {
            float[] vertices =
            {
                -1.0f,  1.0f,  0.0f, 1.0f,
                -1.0f, -1.0f,  0.0f, 0.0f,
                1.0f, -1.0f,  1.0f, 0.0f,
                -1.0f,  1.0f,  0.0f, 1.0f,
                1.0f, -1.0f,  1.0f, 0.0f,
                1.0f,  1.0f,  1.0f, 1.0f
            };

            int Matrix4SizeInBytes = 16 * sizeof(float); // 64 bytes
            int Vector2SizeInBytes = 2 * sizeof(float);  // 8 bytes
            int InstanceDataSize = Matrix4SizeInBytes + Vector2SizeInBytes; // 72 bytes

            //Create VAO
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            
            // VBO for static vertex data (positions & UVs)
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);
            
            // Vertex position attribute (location = 0)
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // UV attribute (location = 1)
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            // Instance VBO (dynamic model matrices & atlas offsets)
            int instanceVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO); 
            GL.BufferData(BufferTarget.ArrayBuffer, 0, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            // Model Matrix (layout = 2, 3, 4, 5)
            int attributeIndex = 2; // Start from index 2
            for (int i = 0; i < 4; i++) // Each row of Matrix4 is a vec4
            {
                GL.VertexAttribPointer(attributeIndex + i, 4, VertexAttribPointerType.Float, false, InstanceDataSize, (IntPtr)(i * Vector4.SizeInBytes));
                GL.EnableVertexAttribArray(attributeIndex + i);
                GL.VertexAttribDivisor(attributeIndex + i, 1); // Per-instance data
            }

            // Atlas Offset (layout = 6)
            int atlasOffsetIndex = attributeIndex + 4;
            GL.VertexAttribPointer(atlasOffsetIndex, 2, VertexAttribPointerType.Float, false, InstanceDataSize, (IntPtr)Matrix4SizeInBytes);
            GL.EnableVertexAttribArray(atlasOffsetIndex);
            GL.VertexAttribDivisor(atlasOffsetIndex, 1); // Per-instance data

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
            InstanceVAOs.Add(path, (vao, instanceVBO, 1));
            return (vao, instanceVBO);
        }
    }
    
    public static (int vao, int vbo) GetInstancedMeshVAO(string path, Mesh mesh)
    {
        //if Instanced VAO already exists
        if (InstanceVAOs.TryGetValue(path, out var cached))
        {
            //Add another user
            cached.users++;
            //Sync to dictionary
            InstanceVAOs[path] = cached;
            return (cached.vao, cached.vbo);
        }
        else
        {
            int Matrix4SizeInBytes = 16 * sizeof(float);

            //Create VAO
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            //Create VBO for unchanging data (Pos, Normal, UV)
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo); //bind VBO
            float[] interleavedData = mesh.GetInterleavedVertexData(); //get Pos, Normal, and UV
            GL.BufferData(BufferTarget.ArrayBuffer, interleavedData.Length * sizeof(float), interleavedData, BufferUsageHint.StaticDraw); //Bind

            //Set up VAO attributes
            int stride = 8 * sizeof(float); // 3 (position) + 3 (normal) + 2 (uv)
            
            // Position attribute (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            
            // Normal attribute (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            // UV attribute (location = 2)
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            //Create EBO for indices
            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Length * sizeof(uint), mesh.Indices, BufferUsageHint.StaticDraw);

            //Create VBO for instanced data (just Matrix4 for each Model Matrix)
            int instanceVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVBO); //bind
            GL.BufferData(BufferTarget.ArrayBuffer, 0, IntPtr.Zero, BufferUsageHint.DynamicDraw); //buffer empty

            int attributeIndex = 3; // Start from index 3 (after position, normal, UV)
            // A Matrix4 consists of 4 vec4s, so we use 4 attribute slots
            for (int i = 0; i < 4; i++)
            {
                GL.VertexAttribPointer(attributeIndex + i, 4, VertexAttribPointerType.Float, false, Matrix4SizeInBytes, (i * Vector4.SizeInBytes));
                GL.EnableVertexAttribArray(attributeIndex + i);
                GL.VertexAttribDivisor(attributeIndex + i, 1); // Set as per-instance data
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            InstanceVAOs.Add(path, (vao, instanceVBO, 1));
            return (vao, instanceVBO);
        }
    }

    public static int GetMeshVAO(string path, Mesh mesh)
    {
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.vao;
        }
        else
        {
            //int vao; configuration for vertex data, offsets, and indices
            //int vbo; unique vertex data (pos, normal, uv)
            //int ebo; indices to connect the vertices into triangles
            
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            float[] interleavedData = mesh.GetInterleavedVertexData();
            GL.BufferData(BufferTarget.ArrayBuffer, interleavedData.Length * sizeof(float), interleavedData, BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float); // 3 (position) + 3 (normal) + 2 (uv)
            
            // Position attribute (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            
            // Normal attribute (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            // UV attribute (location = 2)
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.Indices.Length * sizeof(uint), mesh.Indices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            VAOs.Add(path, (vao, 1));
            return vao;
        }
    }
    
    public static int GetLineVAO(string path, List<Vector3> points)
    {
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.vao;
        }
        else
        {
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(points.Count * Vector3.SizeInBytes), points.ToArray(), BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
            VAOs.Add(path, (vao, 1));
            return vao;
        }
    }
    
    public static int GetSkyboxVAO()
    {
        string path = "skybox";
        
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.vao;
        }
        else
        {
            float[] vertices = 
            {
                -1.0f,  1.0f,
                -1.0f, -1.0f,
                1.0f, -1.0f,
                -1.0f,  1.0f,
                1.0f, -1.0f,
                1.0f,  1.0f
            };
                            
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);
            
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
            VAOs.Add(path, (vao, 1));
            return vao;
        }
    }

    public static int GetUIVAO()
    {
        string path = "ui";
        
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.vao;
        }
        else
        {
            float[] vertices =
            {
                -1.0f,  1.0f,  0.0f, 1.0f,
                -1.0f, -1.0f,  0.0f, 0.0f,
                1.0f, -1.0f,  1.0f, 0.0f,
                -1.0f,  1.0f,  0.0f, 1.0f,
                1.0f, -1.0f,  1.0f, 0.0f,
                1.0f,  1.0f,  1.0f, 1.0f
            };
                            
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);
            
            // Pos
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // UV
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
            VAOs.Add(path, (vao, 1));
            return vao;
        }
    }

    public static void RemoveInstancedVAO(string path)
    {
        if (InstanceVAOs.TryGetValue(path, out var cached))
        {
            cached.users--;

            if (cached.users == 0)
            {
                GL.DeleteVertexArray(cached.vao);
                InstanceVAOs.Remove(path);
            }
            else
            {
                InstanceVAOs[path] = cached;
            }
        }
    }

    public static void RemoveVAO(string path)
    {
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users--;

            if (cached.users == 0)
            {
                GL.DeleteVertexArray(cached.vao);
                VAOs.Remove(path);
            }
            else
            {
                VAOs[path] = cached;
            }
        }
    }
}

