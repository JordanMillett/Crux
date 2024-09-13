using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Crux.Components;

public class FreeLookComponent : Component
{      
    public float yaw = 0;
    public float pitch = 0;
    public float speed = 4f;
    
    public FreeLookComponent(GameObject gameObject) : base(gameObject)
    {
        
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new FreeLookComponent(gameObject);
    }

    public override void Update()
    {
        Move();

        if (GameEngine.Link.CursorState == OpenTK.Windowing.Common.CursorState.Grabbed)
            Look();
    }
    
    public void Look()
    {
        TransformComponent transform = GameEngine.Link.Camera.Transform;
        float sensitivity = 0.1f;

        Vector2 LookInput = new Vector2(GameEngine.Link.MouseState.Delta.X, GameEngine.Link.MouseState.Delta.Y);
        if (GameEngine.Link.IsKeyDown(Keys.Up))
            LookInput -= new Vector2(0, 1) * 15;
        if (GameEngine.Link.IsKeyDown(Keys.Down))
            LookInput += new Vector2(0, 1) * 15;
        if (GameEngine.Link.IsKeyDown(Keys.Left))
            LookInput -= new Vector2(1, 0) * 20;
        if (GameEngine.Link.IsKeyDown(Keys.Right))
            LookInput += new Vector2(1, 0) * 20;
        
        yaw -= MathHelper.DegreesToRadians(LookInput.X * sensitivity);
        pitch += MathHelper.DegreesToRadians(LookInput.Y * sensitivity);
        pitch = MathHelper.DegreesToRadians(MathHelper.Clamp(MathHelper.RadiansToDegrees(pitch), -80f, 80f));

        Quaternion yawRotation = Quaternion.FromAxisAngle(Vector3.UnitY, yaw);
        Quaternion newRotation = yawRotation * Quaternion.FromAxisAngle(Vector3.UnitX, pitch);

        transform.WorldRotation = newRotation;
    }
    
    void Move()
    {
        TransformComponent transform = GameEngine.Link.Camera.Transform;
        Vector3 pos = Vector3.Zero;
        Vector3 forward = transform.Forward;
        Vector3 right = -transform.Right;
        Vector3 up = Vector3.UnitY;

        if (GameEngine.Link.IsKeyDown(Keys.W))
            pos += forward;
        if (GameEngine.Link.IsKeyDown(Keys.S))
            pos -= forward;
        if (GameEngine.Link.IsKeyDown(Keys.A))
            pos -= right;
        if (GameEngine.Link.IsKeyDown(Keys.D))
            pos += right;
        if (GameEngine.Link.IsKeyDown(Keys.Space))
            pos += up; 
        if (GameEngine.Link.IsKeyDown(Keys.LeftShift))
            pos -= up;  
            
        if (GameEngine.Link.MouseState.ScrollDelta.Y > 0)
            speed *= 2;
            
        if (GameEngine.Link.MouseState.ScrollDelta.Y < 0)
            speed /= 2;

        speed = Math.Clamp(speed, 2f, 128f);
            
        transform.WorldPosition += pos * speed * GameEngine.Link.deltaTime;
    }
}
