using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;

namespace Crux.Components;

public class CameraComponent : Component
{      
    private Matrix4 Projection = Matrix4.Zero;
    private Matrix4 View = Matrix4.Zero;
    private int UBO = 0;
    Plane[] planes = new Plane[6];

    private float _nearPlane = 0.1f;
    public float NearPlane 
    { 
        get { return _nearPlane; } 
        set 
        { 
            _nearPlane = value;
            Recalculate(); 
        } 
    }

    private float _farPlane = 200f;
    public float FarPlane 
    { 
        get { return _farPlane; } 
        set 
        { 
            _farPlane = value;
            Recalculate(); 
        } 
    }

    public CameraComponent(GameObject gameObject) : base(gameObject)
    {
        GameEngine.Link.Camera = this;
        
        // Create a UBO
        UBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.UniformBuffer, UBO);
        GL.BufferData(BufferTarget.UniformBuffer, 2 * 16 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw); // 2 mat4 (view + projection)
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        
        // Bind UBO Globally
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, UBO);
        
        //TransformComponent transform = this.Transform;
        //transform.Changed += Recalculate;
        Recalculate();
    }

    public override void Update()
    {
        Recalculate();
    }
    
    public void Recalculate()
    {
        float aspectRatio = (float) GameEngine.Link.Resolution.X / GameEngine.Link.Resolution.Y;
        Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), aspectRatio, NearPlane, FarPlane);

        View = Matrix4.LookAt(
            this.Transform.WorldPosition, // Camera position in world space
            this.Transform.WorldPosition + this.Transform.Forward,     // The point the camera is looking at
            Vector3.UnitY      // The "up" direction for the camera
        );
        
        GL.BindBuffer(BufferTarget.UniformBuffer, UBO);
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, 16 * sizeof(float), ref View.Row0);
        GL.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)(16 * sizeof(float)), 16 * sizeof(float), ref Projection.Row0);
        GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        
        //RecalculateFrustumPlanes(View * Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(5.0f), aspectRatio, NearPlane, FarPlane));
        RecalculateFrustumPlanes(View * Projection);
        GraphicsCache.Tree.RecalculateVisibility();
    }

    void RecalculateFrustumPlanes(Matrix4 m)
    {
        planes[0] = new Plane(m.Column3 + m.Column0);
        planes[1] = new Plane(m.Column3 - m.Column0);
        planes[2] = new Plane(m.Column3 + m.Column1);
        planes[3] = new Plane(m.Column3 - m.Column1);
        planes[4] = new Plane(m.Column3 + m.Column2);
        planes[5] = new Plane(m.Column3 - m.Column2);
        
        for (int i = 0; i < 6; i++)
            planes[i].Normalize();
    }
    
    public bool IsSphereWithinFarPlane(Vector3 sphereCenter, float sphereRadius)
    {
        float distanceToCamera = (sphereCenter - this.Transform.WorldPosition).Length;
        return distanceToCamera <= planes[5].Distance + sphereRadius;
    }

    public bool OutsideOfFrustrum(Vector3 AABBMin, Vector3 AABBMax)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector3 normal = planes[i].Normal;
            float distance = planes[i].Distance;

            // Find the corner of the AABB that is most in the direction of the plane normal
            Vector3 positiveCorner = new Vector3
            (
                normal.X > 0 ? AABBMax.X : AABBMin.X,
                normal.Y > 0 ? AABBMax.Y : AABBMin.Y,
                normal.Z > 0 ? AABBMax.Z : AABBMin.Z
            );

            // If the positive corner is outside the plane, the entire AABB is outside
            if (Vector3.Dot(normal, positiveCorner) + distance < 0)
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new CameraComponent(gameObject);
    }
}
