namespace Game.Logic;

public class FaceCamera : Component
{      
    public float force = 10f;

    public FaceCamera(GameObject gameObject) : base(gameObject)
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
        return new FaceCamera(gameObject);
    }

    public override void Update()
    {
        // Get the position of the camera and this object
        Vector3 objectPosition = this.Transform.WorldPosition;
        Vector3 cameraPosition = GameEngine.Link.Camera!.Transform.WorldPosition;

        // Compute the direction vector (ignore Y axis)
        Vector3 direction = new Vector3(cameraPosition.X - objectPosition.X, 0, cameraPosition.Z - objectPosition.Z);
        
        // Normalize the direction vector
        if (direction.LengthSquared > 0)
        {
            direction.Normalize();
            
            // Calculate the new Y-axis rotation angle (look at direction)
            float angle = MathHelper.RadiansToDegrees((float)Math.Atan2(direction.X, direction.Z));

            // Apply only Y rotation
            this.Transform.LocalEulerAngles = new Vector3(0f, 0f, -angle);
        }
    }

}
