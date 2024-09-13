using OpenTK.Graphics.OpenGL4;

namespace Crux.Components;

public class LightComponent : Component
{       
    static int totalLights = 0;
    static int SSBO = -1;
    int Index = 0;
    
    public const int MAX_LIGHTS = 4;

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
    
    private float _intensity = 1f;
    public float Intensity 
    { 
        get { return _intensity; } 
        set 
        { 
            _intensity = value;
            Recalculate(); 
        } 
    }
    
    public static int GetLightByteSize()
    {
        return 12 * sizeof(float);
    }
    
    public LightComponent(GameObject gameObject) : base(gameObject)
    {
        Index = totalLights;
        totalLights++;
        
        if (SSBO == -1)
        {                
            SSBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, SSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, MAX_LIGHTS * GetLightByteSize(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, SSBO);
        }

        TransformComponent transform = this.Transform;
        transform.Changed += Recalculate;
        Recalculate();
    }
    
    public void Recalculate()
    {
        TransformComponent transform = this.Transform;
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, SSBO);
        
        int offset = Index * GetLightByteSize();
        float[] lightData = new float[]
        {
            transform.WorldPosition.X, transform.WorldPosition.Y, transform.WorldPosition.Z, 0.0f, //padding
            Hue.R, Hue.G, Hue.B, Hue.A,
            Intensity, 0f, 0f, 0f //padded
        };
        
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer,
            offset,
            lightData.Length * sizeof(float),
            lightData);
            
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");
        sb.AppendLine($"Light Index: { Index }");
        sb.AppendLine($"Hue: { Hue }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new LightComponent(gameObject);
    }
}
