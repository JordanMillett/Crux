using CruxEngine.Utilities.Helpers;
using CruxEngine.Utilities.IO;
using OpenTK.Graphics.OpenGL4;

namespace CruxEngine.Graphics;

public class SceneLightingJsonPreset : JsonPreset
{
    public Color4 AmbientColor { get; init; } = ColorHelper.HexToColor4("FFFFFF");
    public Color4 FogColor { get; init; } = ColorHelper.HexToColor4("a3a3ff");
    public Color4 SunColor { get; init; } = ColorHelper.HexToColor4("FFFFFF");
    public float SunIntensity { get; init; } = 1f;
    public float AlphaFadeStart { get; init; } = 75f;
    public float AlphaFadeEnd { get; init; } = 100f;
    public float FogStart { get; init; } = 10f;
    public float FogEnd { get; init; } = 100f;
}

public class SceneLighting
{
    static SceneLighting Active = null!;

    static int UBO = -1;
    
    private Color4 _ambientColor = ColorHelper.Missing;
    public Color4 AmbientColor
    { 
        get { return _ambientColor; } 
        set 
        { 
            _ambientColor = value;
            Recalculate(); 
        } 
    }
    
    private Color4 _fogColor = ColorHelper.Missing;
    public Color4 FogColor
    { 
        get { return _fogColor; } 
        set 
        { 
            _fogColor = value;
            Recalculate(); 
        } 
    }
    
    private Color4 _sunColor = ColorHelper.Missing;
    public Color4 SunColor
    { 
        get { return _sunColor; } 
        set 
        { 
            _sunColor = value;
            Recalculate(); 
        } 
    }

    private float _sunIntensity = 1f;
    public float SunIntensity 
    { 
        get { return _sunIntensity; } 
        set 
        { 
            _sunIntensity = value;
            Recalculate(); 
        } 
    }

    private float _alphaFadeStart = 75f;
    public float AlphaFadeStart 
    { 
        get { return _alphaFadeStart; } 
        set 
        { 
            _alphaFadeStart = value;
            Recalculate(); 
        } 
    }

    private float _alphaFadeEnd = 100f;
    public float AlphaFadeEnd 
    { 
        get { return _alphaFadeEnd; } 
        set 
        { 
            _alphaFadeEnd = value;
            Recalculate(); 
        } 
    }

    private float _fogStart = 10f;
    public float FogStart 
    { 
        get { return _fogStart; } 
        set 
        { 
            _fogStart = value;
            Recalculate(); 
        } 
    }

    private float _fogEnd = 100f;
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
        if(Active == null)
            Active = this;

        if(Active != this)
            return;

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

    public void Apply()
    {
        Active = this;
        Recalculate();
    }
    
    public void Recalculate()
    {
        if(Active != this)
            return;

        Crux.Camera.FarPlane = AlphaFadeEnd;

        GL.BindBuffer(BufferTarget.UniformBuffer, UBO);

        float[] lightData = new float[] //20 bytes total
        {
            SunDirection.X, SunDirection.Y, SunDirection.Z, SunIntensity, //SunIntensity was padding
            SunColor.R, SunColor.G, SunColor.B, SunColor.A,
            AmbientColor.R, AmbientColor.G, AmbientColor.B, AmbientColor.A,
            FogColor.R, FogColor.G, FogColor.B, FogColor.A,
            AlphaFadeStart, AlphaFadeEnd, FogStart, FogEnd
        };
        
        GL.BufferSubData(BufferTarget.UniformBuffer,
            0,
            lightData.Length * sizeof(float),
            lightData);
            
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
    }
}
