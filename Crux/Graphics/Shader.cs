using OpenTK.Graphics.OpenGL4;
using Crux.Utilities.Helpers;

namespace Crux.Graphics;

public class Shader
{
    private readonly int _programId;
    private readonly int _vertexShaderId;
    private readonly int _fragmentShaderId;
    private readonly int _textureId;
    
    public string VertexShaderPath { get; init; }
    public string FragmentShaderPath { get; init; }
    public string ColorTexturePath { get; init; }

    //Standard uniforms in every shader
    public Color4 TextureHue { get; set; } = Color4.White;

    //Varied uniforms across different shaders
    private Dictionary<string, object> pendingUniformUpdates = [];
    private Dictionary<string, int> uniformLocations = [];

    public bool Instanced = false;

    public Shader(string vertexShaderPath, string fragmentShaderPath, string colorTexturePath, bool instanced)
    {
        VertexShaderPath = vertexShaderPath;
        FragmentShaderPath = fragmentShaderPath;
        ColorTexturePath = colorTexturePath;
        Instanced = instanced;
        
        _vertexShaderId = GraphicsCache.GetVertexShader(VertexShaderPath, Instanced);
        _fragmentShaderId = GraphicsCache.GetFragmentShader(FragmentShaderPath, Instanced);
        _programId = GraphicsCache.GetProgram((_vertexShaderId, _fragmentShaderId));
        if(!string.IsNullOrEmpty(ColorTexturePath))
            _textureId = GraphicsCache.GetTexture(ColorTexturePath);
    }

    public Shader Clone()
    {
        Shader clone = new Shader
        (
            VertexShaderPath,
            FragmentShaderPath,
            ColorTexturePath,
            Instanced
        )
        {
            //Uniforms
            TextureHue = this.TextureHue
        };

        return clone;
    }

    public void Bind()
    {
        //Set standard uniforms
        SetUniform("TextureHue", TextureHue); 

        GL.UseProgram(_programId);

        //Apply standard and varied uniforms
        foreach (var uniform in pendingUniformUpdates)
            ApplyUniform(uniform.Key, uniform.Value);
        pendingUniformUpdates.Clear();
        
        if(!string.IsNullOrEmpty(ColorTexturePath))
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
        }
    }
    
    public void Unbind()
    {
        GL.UseProgram(0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    public void Delete()
    {
        GraphicsCache.RemoveVertexUser(VertexShaderPath);
        GraphicsCache.RemoveFragmentUser(FragmentShaderPath);
        GraphicsCache.RemoveProgramUser((_vertexShaderId, _fragmentShaderId));
        GraphicsCache.RemoveTextureUser(ColorTexturePath);
    }

    public void SetUniform(string uniformName, object value)
    {
        pendingUniformUpdates[uniformName] = value;
    }
    
    void ApplyUniform(string uniformName, object value)
    {
        int uniformLocation;
        if (uniformLocations.TryGetValue(uniformName, out var cached))
        {
            uniformLocation = cached;
        }else
        {
            uniformLocation = GL.GetUniformLocation(_programId, uniformName);
            uniformLocations.Add(uniformName, uniformLocation);
        }
        
        if (uniformLocation == -1)
        {
            Logger.LogWarning($"Uniform '{uniformName}' does not exist in fragment shader '{FragmentShaderPath}' or vertex shader '{VertexShaderPath}'.");
            return;
        }

        if (value is Color4 color)
        {
            GL.Uniform4(uniformLocation, color.R, color.G, color.B, color.A);
        }
        else if (value is Matrix4 matrix)
        {
            float[] matrixArray = MatrixHelper.Matrix4ToArray(matrix);
            GL.UniformMatrix4(uniformLocation, 1, false, matrixArray);
        }else if (value is Vector2 vector)
        {
            GL.Uniform2(uniformLocation, vector);
        }else if (value is int[] intArray)
        {
            GL.Uniform1(uniformLocation, intArray.Length, intArray);
        }
    }
}
