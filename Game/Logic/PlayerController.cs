using OpenTK.Windowing.GraphicsLibraryFramework;
using Crux.Components;
using Crux.Physics;

namespace Game.Logic;

public class PlayerController : Component
{      
    PhysicsComponent physics;

    public PlayerController(GameObject gameObject) : base(gameObject)
    {
        physics = GetComponent<PhysicsComponent>();
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new PlayerController(gameObject);
    }

    public override void Update()
    {
        Move();

        if (GameEngine.Link.CursorState == OpenTK.Windowing.Common.CursorState.Grabbed)
            Look();
    }
    
    public void Look()
    {
        TransformComponent cam = GameEngine.Link.Camera.Transform;

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

        float yawDelta = -LookInput.X * sensitivity;
        Quaternion yawRotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(yawDelta));
        Transform.WorldRotation = yawRotation * Transform.WorldRotation;

        Vector3 camEuler = cam.LocalEulerAngles;
        camEuler.X += LookInput.Y * sensitivity;
        camEuler.X = MathHelper.Clamp(camEuler.X, -80f, 80f);
        cam.LocalEulerAngles = camEuler;
    }
    
    void Move()
    {
        float force = 10f;
        float mult = 1f;
        if (GameEngine.Link.IsKeyDown(Keys.LeftShift))
        {
            //mult *= 1.5f;
            mult *= 5f;
        }
        force *= mult;

        Vector3 pos = Vector3.Zero;
        Vector3 forward = Transform.Forward;
        Vector3 right = -Transform.Right;

        if (GameEngine.Link.IsKeyDown(Keys.W))
            pos += forward * force;
        if (GameEngine.Link.IsKeyDown(Keys.S))
            pos -= forward * force;
        if (GameEngine.Link.IsKeyDown(Keys.A))
            pos -= right * force;
        if (GameEngine.Link.IsKeyDown(Keys.D))
            pos += right * force;
    
        if(physics.Velocity.Length < 3f * mult)
            physics.AddForce(pos * GameEngine.Link.deltaTime, true); 

        /*
        if (GameEngine.Link.IsKeyPressed(Keys.Space) && CanJump())
        {
            lastJumped = GameEngine.Link.totalTime;
            physics.AddForce(Vector3.UnitY * GameEngine.Link.deltaTime * 200f, true); 
        }
        */
    }

    float lastJumped = 0f;

    bool CanJump()
    {
        if(GameEngine.Link.totalTime < lastJumped + 0.25f)
            return false;

        Ray ray = new Ray(this.Transform.WorldPosition, -this.Transform.Up, 0.95f);
        return PhysicsSystem.Raycast(ray, out _);
    }
}
