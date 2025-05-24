using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Components;

namespace Crux.Core;

public abstract class Scene
{
    static int UBO = -1;

    public Shader Skybox;

    private readonly MeshBuffer skyboxBuffer;

    public static GameEngine Engine
    {
        get { return GameEngine.Link; }
    }

    public static CameraComponent MainCamera
    {
        get { return GameEngine.Link.Camera!; }
    }
    
    private Color4 _ambient = Color4.White;
    public Color4 Ambient 
    { 
        get { return _ambient; } 
        set 
        { 
            _ambient = value;
            Recalculate(); 
        } 
    }
    
    private Color4 _fog = new Color4(0.639215686f, 0.639215686f, 1.0f, 1.0f);
    public Color4 Fog 
    { 
        get { return _fog; } 
        set 
        { 
            _fog = value;
            Recalculate(); 
        } 
    }
    
    private Color4 _hue = Color4.White;
    public Color4 Hue 
    { 
        get { return _hue; } 
        set 
        { 
            _hue = value;
            Recalculate(); 
        } 
    }

    private float _fadeStart = 175f;
    public float FadeStart 
    { 
        get { return _fadeStart; } 
        set 
        { 
            _fadeStart = value;
            Recalculate(); 
        } 
    }

    private float _fadeEnd = 200f;
    public float FadeEnd 
    { 
        get { return _fadeEnd; } 
        set 
        { 
            _fadeEnd = value;
            Recalculate(); 
        } 
    }

    private float _fogStart = 20f;
    public float FogStart 
    { 
        get { return _fogStart; } 
        set 
        { 
            _fogStart = value;
            Recalculate(); 
        } 
    }

    private float _fogEnd = 200f;
    public float FogEnd 
    { 
        get { return _fogEnd; } 
        set 
        { 
            _fogEnd = value;
            Recalculate(); 
        } 
    }

    private Vector3 _dir = new Vector3(-0.5f, -1.0f, -0.5f);
    public Vector3 Dir 
    { 
        get { return _dir; } 
        set 
        { 
            _dir = value;
            Recalculate(); 
        } 
    }
    
    public static int GetDataByteSize()
    {
        return 20 * sizeof(float);
    }
    
    public Scene()
    {
        //string materialPath = "Crux/Assets/Materials/Skybox.json";
        Skybox = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Unlit_2D_Skybox, false);

        skyboxBuffer = GraphicsCache.GetInstancedQuadBuffer(GraphicsCache.QuadBufferType.ui_with_no_uvs);
        
        if (UBO == -1)
        {                
            UBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.UniformBuffer, UBO);
            GL.BufferData(BufferTarget.UniformBuffer, GetDataByteSize(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 1, UBO);
        }

        Recalculate();
    }

    public abstract void Start();
    public abstract void Update();
    
    public void RenderSkybox()
    {
        GL.DepthMask(false);

        Skybox.Bind();
        
        GL.BindVertexArray(skyboxBuffer.VAO);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        //GraphicsCache.DrawCallsThisFrame++;

        GL.BindVertexArray(0);

        Skybox.Unbind();
        
        GL.DepthMask(true); 
    }
    
    public void Recalculate()
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, UBO);

        float[] lightData = new float[] //20 bytes total
        {
            Dir.X, Dir.Y, Dir.Z, 0.0f, //padding
            Hue.R, Hue.G, Hue.B, Hue.A,
            Ambient.R, Ambient.G, Ambient.B, Ambient.A,
            Fog.R, Fog.G, Fog.B, Fog.A,
            FogStart, FogEnd, FadeStart, FadeEnd
        };
        
        GL.BufferSubData(BufferTarget.UniformBuffer,
            0,
            lightData.Length * sizeof(float),
            lightData);
            
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
    }
}
