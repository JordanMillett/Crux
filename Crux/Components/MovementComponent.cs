namespace Crux.Components;

public class MovementComponent : Component
{
    Vector3 origin;
    
    public MovementComponent(GameObject gameObject) : base(gameObject)
    {
        origin = this.Transform.WorldPosition;
    }

    public override void Update()
    {
        TransformComponent transform = this.Transform;

        transform.WorldRotation *= Quaternion.FromAxisAngle(Vector3.UnitY, GameEngine.Link.deltaTime * 0.25f);
        transform.WorldRotation *= Quaternion.FromAxisAngle(Vector3.UnitZ, GameEngine.Link.deltaTime * 0.25f);

        transform.WorldPosition = Vector3.Lerp(origin, origin + new Vector3(0f, 2f, 0f), (float) (Math.Sin(GameEngine.Link.totalTime * 0.25f) * 0.5f) + 0.5f);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new MovementComponent(gameObject);
    }
}
