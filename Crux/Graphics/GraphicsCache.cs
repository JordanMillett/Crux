using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using Crux.Utilities.IO;
using Crux.Physics;
using Crux.Utilities.Helpers;

namespace Crux.Graphics;

public static class GraphicsCache
{
    static Dictionary<string, (int id, int users)> Textures = new();
    
    static Dictionary<(string path, bool instanced), (int id, int users)> Vertex = new();
    static Dictionary<(string path, bool instanced), (int id, int users)> Fragment = new();
    static Dictionary<(int vertId, int fragId), (int id, int users)> Programs = new();
    
    public static Dictionary<string, (MeshBuffer meshBuffer, int users)> VAOs = new();

    public static int DrawCallsThisFrame = 0;
    public static int TrianglesThisFrame = 0;
    public static int LinesThisFrame = 0;
    public static float FramesPerSecond = 0f;

    public static Octree Tree;

    public static string common_vert;
    public static string common_frag;

    static GraphicsCache()
    {
        Tree = new Octree(new Vector3(-500, -500, -500), new Vector3(500, 500, 500), 5, "Visibility Octree");
        common_vert = AssetHandler.ReadAssetInFull("Crux/Assets/Shaders/Required/common_vert.glsl");
        common_frag = AssetHandler.ReadAssetInFull("Crux/Assets/Shaders/Required/common_frag.glsl");
    }
    
    public static string GetShortInfo()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"FPS - {FramesPerSecond:F2}");
        sb.AppendLine($"Draw Calls - {DrawCallsThisFrame}");
        sb.AppendLine($"Triangles - {TrianglesThisFrame}");
        sb.AppendLine($"Lines - {LinesThisFrame}");

        sb.AppendLine($"Unique VAOs - {VAOs.Count}x");
        sb.AppendLine($"Unique Textures - {Textures.Count}x");
        sb.AppendLine($"Unique Shader Programs - {Programs.Count}x");

        return sb.ToString();
    }

    public static string GetFullInfo()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"FPS - {FramesPerSecond:F2}");
        sb.AppendLine($"Draw Calls - {DrawCallsThisFrame}");
        sb.AppendLine($"Triangles - {TrianglesThisFrame}");
        sb.AppendLine($"Lines - {LinesThisFrame}");

        sb.AppendLine($"Unique VAOs - {VAOs.Count}x");
        foreach (var entry in VAOs)
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
    
    public static int GetVertexShader(string path, bool useInstancing)
    {
        if (Vertex.TryGetValue((path, useInstancing), out var cached))
        {
            cached.users++;
            Vertex[(path, useInstancing)] = cached;
            return cached.id;
        }else
        {
            int id = GL.CreateShader(ShaderType.VertexShader);
            string contents = AssetHandler.ReadAssetInFull(path);
            if (useInstancing)
                contents = contents.Substring(0, 13) + "#define INSTANCED\n" + contents.Substring(13); //must define version first

            contents = contents.Replace("#include <common_vert.glsl>", "\n" + common_vert);

            GL.ShaderSource(id, contents);
            GL.CompileShader(id);
            string vertexShaderLog = GL.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(vertexShaderLog))
            {
                //GL.DeleteShader(id);
                throw new Exception($"Error compiling vertex shader {path}: {vertexShaderLog}");
            }

            Vertex.Add((path, useInstancing), (id, 1));

            return id;
        }
    }
    
    public static void RemoveVertexUser(string path, bool useInstancing)
    {
        if (Vertex.TryGetValue((path, useInstancing), out var cached))
        {
            cached.users--;
            
            if(cached.users == 0)
            {
                Vertex.Remove((path, useInstancing));
                GL.DeleteShader(cached.id);
            }else
            {
                Vertex[(path, useInstancing)] = cached; 
            }
        }
    }
    
    public static int GetFragmentShader(string path, bool useInstancing)
    {
        if (Fragment.TryGetValue((path, useInstancing), out var cached))
        {
            cached.users++;
            Fragment[(path, useInstancing)] = cached;
            return cached.id;
        }else
        {
            int id = GL.CreateShader(ShaderType.FragmentShader);
            string contents = AssetHandler.ReadAssetInFull(path);
            if (useInstancing)
                contents = contents.Substring(0, 13) + "#define INSTANCED\n" + contents.Substring(13); //must define version first
                
            contents = contents.Replace("#include <common_frag.glsl>", "\n" + common_frag);

            GL.ShaderSource(id, contents);
            GL.CompileShader(id);
            string fragmentShaderLog = GL.GetShaderInfoLog(id);
            if (!string.IsNullOrEmpty(fragmentShaderLog))
            {
                //GL.DeleteShader(id);
                throw new Exception($"Error compiling fragment shader {path}: {fragmentShaderLog}");
            }

            Fragment.Add((path, useInstancing), (id, 1));

            return id;
        }
    }
    
    public static void RemoveFragmentUser(string path, bool useInstancing)
    {
        if (Fragment.TryGetValue((path, useInstancing), out var cached))
        {
            cached.users--;
            
            if(cached.users == 0)
            {
                Fragment.Remove((path, useInstancing));
                GL.DeleteShader(cached.id);
            }else
            {
                Fragment[(path, useInstancing)] = cached; 
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

    public enum QuadBufferType
    {
        ui_with_color,
        ui_with_atlas,
        ui_with_no_uvs
    }

    public static MeshBuffer GetInstancedQuadBuffer(QuadBufferType bufferType)
    {
        string path = bufferType switch
        {
            QuadBufferType.ui_with_color => "ui_container",
            QuadBufferType.ui_with_atlas => "ui_text",
            QuadBufferType.ui_with_no_uvs => "ui_skybox",
            _ => "unknown"
        };
        
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.meshBuffer;
        }
        else
        {
            MeshBuffer meshBuffer = new();   

            VertexAttribute positionAttribute = VertexAttributeHelper.ConvertToAttribute(0, "inPosition", Shapes.QuadVertices);
            VertexAttribute uvsAttribute = VertexAttributeHelper.ConvertToAttribute(1, "inUV", Shapes.QuadUVs);
            VertexAttribute[] staticAttributes = [positionAttribute, uvsAttribute]; 
            meshBuffer.GenStaticVBO(staticAttributes);
            
            (int, Type)[] dynamicAttributes = bufferType switch
            {
                QuadBufferType.ui_with_color => new (int, Type)[]
                {
                    (3, typeof(Matrix4)),
                    (7, typeof(Vector4))
                },
                QuadBufferType.ui_with_atlas => new (int, Type)[]
                {
                    (2, typeof(Matrix4)),
                    (6, typeof(Vector2))
                },
                _ => null!
            };

            if(dynamicAttributes != null)
                meshBuffer.GenDynamicVBO(dynamicAttributes);
            
            VAOs.Add(path, (meshBuffer, 1));            
            return meshBuffer;
        }
    }

    public static MeshBuffer GetInstancedLineBuffer(string path, Vector3[] vertices)
    {
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.meshBuffer;
        }
        else
        {
            
            MeshBuffer meshBuffer = new();   

            VertexAttribute positionAttribute = VertexAttributeHelper.ConvertToAttribute(0, "inPosition", vertices);
            VertexAttribute[] staticAttributes = [positionAttribute]; 
            meshBuffer.GenStaticVBO(staticAttributes);
    
            meshBuffer.GenDynamicVBO(new (int, Type)[]
            {
                (3, typeof(Matrix4)),
                (7, typeof(Vector4))
            });
            
            VAOs.Add(path, (meshBuffer, 1));            
            return meshBuffer;
        }
    }
    
    public static MeshBuffer GetInstancedMeshBuffer(string path, Mesh mesh)
    {
        //if Instanced VAO already exists
        if (VAOs.TryGetValue(path, out var cached))
        {
            //Add another user
            cached.users++;
            //Sync to dictionary
            VAOs[path] = cached;
            return cached.meshBuffer;
        }
        else
        {
            MeshBuffer meshBuffer = new();   

            VertexAttribute[] staticAttributes = mesh.GetSeparatedData();
            meshBuffer.GenStaticVBO(staticAttributes);

            meshBuffer.GenEBO(mesh.Indices);

            meshBuffer.GenDynamicVBO(new (int, Type)[]
            {
                (3, typeof(Matrix4))
            });

            VAOs.Add(path, (meshBuffer, 1));
            return meshBuffer;
        }
    }

    public static MeshBuffer GetMeshBuffer(string path, Mesh mesh)
    {
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.meshBuffer;
        }
        else
        {
            MeshBuffer meshBuffer = new();            

            VertexAttribute[] staticAttributes = mesh.GetSeparatedData();
            meshBuffer.GenStaticVBO(staticAttributes);

            meshBuffer.GenEBO(mesh.Indices);            

            VAOs.Add(path, (meshBuffer, 1));
            return meshBuffer;
        }
    }
    
    public static MeshBuffer GetLineBuffer(string path, Vector3[] vertices)
    {
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.meshBuffer;
        }
        else
        {
            MeshBuffer meshBuffer = new();  

            VertexAttribute positionAttribute = VertexAttributeHelper.ConvertToAttribute(0, "inPosition", vertices);
            VertexAttribute[] staticAttributes = [positionAttribute]; 
            meshBuffer.GenStaticVBO(staticAttributes);
            
            VAOs.Add(path, (meshBuffer, 1));
            return meshBuffer;
        }
    }
    
    [Obsolete("Feature not maintained")]
    public static MeshBuffer GetSkyboxBuffer()
    {
        string path = "skybox";
        
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.meshBuffer;
        }
        else
        {
            MeshBuffer meshBuffer = new();  

            VertexAttribute positionAttribute = VertexAttributeHelper.ConvertToAttribute(0, "inPosition", Shapes.QuadVertices);
            VertexAttribute[] staticAttributes = [positionAttribute]; 
            meshBuffer.GenStaticVBO(staticAttributes);
            
            VAOs.Add(path, (meshBuffer, 1));
            return meshBuffer;
        }
    }
    
    [Obsolete("Feature not maintained")]
    public static MeshBuffer GetUIBuffer()
    {
        string path = "ui";
        
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users++;
            VAOs[path] = cached;
            return cached.meshBuffer;
        }
        else
        {
            MeshBuffer meshBuffer = new();  

            VertexAttribute positionAttribute = VertexAttributeHelper.ConvertToAttribute(0, "inPosition", Shapes.QuadVertices);
            VertexAttribute uvsAttribute = VertexAttributeHelper.ConvertToAttribute(1, "inUV", Shapes.QuadUVs);
            VertexAttribute[] staticAttributes = [positionAttribute, uvsAttribute]; 
            meshBuffer.GenStaticVBO(staticAttributes);
            
            VAOs.Add(path, (meshBuffer, 1));
            return meshBuffer;
        }
    }

    public static void RemoveBuffer(string path)
    {
        if (VAOs.TryGetValue(path, out var cached))
        {
            cached.users--;

            if (cached.users == 0)
            {
                GL.DeleteVertexArray(cached.meshBuffer.VAO);
                VAOs.Remove(path);
            }
            else
            {
                VAOs[path] = cached;
            }
        }
    }
}

