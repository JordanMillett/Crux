using CruxEngine;
using CruxEngine.Components;


namespace Game.Logic;

public class Enemy : Component
{      
    public float force = 10f;
    private Timer _timer;

    public Enemy(GameObject gameObject) : base(gameObject)
    {
        TimerCallback callback = state => 
        {
            PhysicsComponent phys = GetComponent<PhysicsComponent>();

            // Local torque: X = pitch, Y = yaw, Z = roll
            Vector3 localTorque = new Vector3(-5f, 0f, 0f); // pitch up

            // Convert to world space manually using Transform axes
            Vector3 worldTorque =
                Transform.Right * localTorque.X +
                Transform.Up * localTorque.Y +
                Transform.Forward * localTorque.Z;

            // Apply torque
            phys.AddTorque(worldTorque, true);

            phys.AddForce(this.Transform.Forward * 10f);
        };

        // Create timer
         _timer = new Timer(
            callback,
            null,
            3000,      // start immediately
            3000    // repeat every 2 seconds
        );
    }

     //float forceStrength = 0.1f;
    //phys.AddForce(forward * forceStrength);
    
    //MAKE THIS BETTER TOO THIS IS WRONG, DONT NEED THIS MUCH BOILERPLATE
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    //DO SOMETHING BETTER WITH THIS
    public override Component Clone(GameObject gameObject)
    {
        return new Enemy(gameObject);
    }

    /*
    //ACTIVE SCENE PLAYER GET THAT INFO EASILY THROUGH CRUX.activescene.player
    //CROSShair CUI align center show image
    public override void Update()
    {
        //fixed timestep?
        //Crux.Engine.fixedDeltaTime
        this.Transform.WorldPosition += Vector3.Normalize((Crux.Camera.Transform.WorldPosition - this.Transform.WorldPosition)) * 0.025f;
    }
    */
    /*
    public override void Update()
    {
        PhysicsComponent phys = GetComponent<PhysicsComponent>();
        if (phys == null) return;

        Vector3 objectPosition = Transform.WorldPosition;
        Vector3 cameraPosition = Crux.Camera.Transform.WorldPosition;

        // Direction to target (ignore Y)
        Vector3 toTarget = cameraPosition - objectPosition;
        toTarget.Y = 0f;
        if (toTarget.LengthSquared == 0f) return;
        toTarget.Normalize();

        // Current forward (ignore Y)
        Vector3 forward = Transform.Forward;
        forward.Y = 0f;
        forward.Normalize();

        // Compute signed angle between forward and target direction
        float angleError = (float)Math.Atan2(
            Vector3.Cross(forward, toTarget).Y,
            Vector3.Dot(forward, toTarget)
        ); // radians

        // Torque strength and damping
        float torqueStrength = 0.05f; // tune
        float damping = 0.05f;

        // Apply torque around Y axis
        phys.AddTorque(new Vector3(0f, angleError * torqueStrength - phys.AngularVelocity.Y * damping, 0f));

        // Move forward along current facing
        float forceStrength = 0.1f;
        phys.AddForce(forward * forceStrength);

    }
    */

    public override void Update()
{
    PhysicsComponent phys = GetComponent<PhysicsComponent>();
    if (phys == null) return;

    Vector3 objectPosition = Transform.WorldPosition;
    Vector3 cameraPosition = Crux.Camera.Transform.WorldPosition;

    // Direction to target
    Vector3 toTarget = cameraPosition - objectPosition;
    if (toTarget.LengthSquared == 0f) return;
    toTarget.Normalize();

    // Current forward
    Vector3 forward = Transform.Forward;
    forward.Normalize();

    // Compute rotation axis and angle
    Vector3 rotationAxis = Vector3.Cross(forward, toTarget);
    float sinAngle = rotationAxis.Length;
    float cosAngle = Vector3.Dot(forward, toTarget);
    float angle = (float)Math.Atan2(sinAngle, cosAngle); // radians

    if (sinAngle > 0.0001f) // avoid divide by zero
        rotationAxis /= sinAngle; // normalize axis

    // Torque strength and damping
    float torqueStrength = 0.05f; // tune
    float damping = 0.05f;

    // Apply torque along rotation axis
    Vector3 desiredTorque = rotationAxis * angle * torqueStrength;
    Vector3 dampingTorque = -phys.AngularVelocity * damping;
    phys.AddTorque(desiredTorque + dampingTorque);

    // Move forward along current facing
    //float forceStrength = 0.1f;
    //phys.AddForce(forward * forceStrength);
}



}
