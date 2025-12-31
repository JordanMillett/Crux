using OpenTK.Graphics.OpenGL4;

namespace CruxEngine.Graphics;

public class SceneLighting
{
    static int UBO = -1;
    
    private Color4 _ambientColor = Color4.White;
    public Color4 AmbientColor
    { 
        get { return _ambientColor; } 
        set 
        { 
            _ambientColor = value;
            Recalculate(); 
        } 
    }
    
    private Color4 _fogColor = new Color4(0.639215686f, 0.639215686f, 1.0f, 1.0f);
    public Color4 FogColor
    { 
        get { return _fogColor; } 
        set 
        { 
            _fogColor = value;
            Recalculate(); 
        } 
    }
    
    private Color4 _sunColor = Color4.White;
    public Color4 SunColor
    { 
        get { return _sunColor; } 
        set 
        { 
            _sunColor = value;
            Recalculate(); 
        } 
    }

    private float _alphaFadeStart = 175f;
    public float AlphaFadeStart 
    { 
        get { return _alphaFadeStart; } 
        set 
        { 
            _alphaFadeStart = value;
            Recalculate(); 
        } 
    }

    private float _alphaFadeEnd = 200f;
    public float AlphaFadeEnd 
    { 
        get { return _alphaFadeEnd; } 
        set 
        { 
            _alphaFadeEnd = value;
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

    private Vector3 _sunDirection = new Vector3(-0.5f, -1.0f, -0.5f);
    public Vector3 SunDirection 
    { 
        get { return _sunDirection; } 
        set 
        { 
            _sunDirection = value;
            Recalculate(); 
        } 
    }
    
    public static int GetDataByteSize()
    {
        return 20 * sizeof(float);
    }
    
    public SceneLighting()
    {   
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
    
    public void Recalculate()
    {
        GL.BindBuffer(BufferTarget.UniformBuffer, UBO);

        float[] lightData = new float[] //20 bytes total
        {
            SunDirection.X, SunDirection.Y, SunDirection.Z, 0.0f, //padding
            SunColor.R, SunColor.G, SunColor.B, SunColor.A,
            AmbientColor.R, AmbientColor.G, AmbientColor.B, AmbientColor.A,
            FogColor.R, FogColor.G, FogColor.B, FogColor.A,
            FogStart, FogEnd, AlphaFadeStart, AlphaFadeEnd
        };
        
        GL.BufferSubData(BufferTarget.UniformBuffer,
            0,
            lightData.Length * sizeof(float),
            lightData);
            
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
    }
}
