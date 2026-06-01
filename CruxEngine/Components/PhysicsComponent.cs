using CruxEngine.Physics;

namespace CruxEngine.Components;

public class PhysicsComponent : Component
{     
    public Vector3 Velocity = Vector3.Zero;
    public Vector3 AngularVelocity = Vector3.Zero;
    public float LinearDrag = 0.5f;
    public float AngularDrag = 0.5f;
    public float Mass = 1f;
    public float InverseMass => 1f/ Mass;

    public bool DisableRotation = false;

    private readonly ColliderComponent col;
    
    public PhysicsComponent(GameObject gameObject): base(gameObject)
    {
        col = GetComponent<ColliderComponent>();
        PhysicsSystem.RegisterPhysicsObject(col, this);
    }

    public override void OnDelete()
    {
        PhysicsSystem.UnregisterPhysicsObject(col, this);
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        PhysicsComponent clone = new PhysicsComponent(gameObject);
        return clone;
    }
    
    public void Integrate()
    {
        Velocity += PhysicsSystem.Gravity * Crux.Engine.fixedDeltaTime;
        GameObject.Transform.WorldPosition += Velocity * Crux.Engine.fixedDeltaTime;
        Velocity *= MathF.Exp(-LinearDrag * Crux.Engine.fixedDeltaTime);

        if (!DisableRotation && AngularVelocity.Length > 0f)
        {
            AngularVelocity *= MathF.Exp(-AngularDrag * Crux.Engine.fixedDeltaTime);
            Quaternion deltaRot = Quaternion.FromAxisAngle(Vector3.Normalize(AngularVelocity), AngularVelocity.Length * Crux.Engine.fixedDeltaTime);
            GameObject.Transform.WorldRotation = deltaRot * GameObject.Transform.WorldRotation;
        }
    }

    //contactPoint is in world space
    public void RespondToCollision(Vector3 contactPoint, Vector3 resolution, PhysicsComponent other)
    {   
        bool otherHasPhysics = other != null;
        float totalInverseMass = InverseMass + (otherHasPhysics ? other!.InverseMass : 0f);

        float massPercentA = InverseMass / totalInverseMass;
        float massPercentB = otherHasPhysics ? (other!.InverseMass / totalInverseMass) : 0f;

        Vector3 normal = Vector3.Normalize(resolution);

        Vector3 rA = contactPoint - GameObject.Transform.WorldPosition;
        Vector3 contactVelocityA = Velocity + Vector3.Cross(AngularVelocity, rA);

        Vector3 rB = Vector3.Zero;
        Vector3 contactVelocityB = Vector3.Zero;
        if (otherHasPhysics)
        {
            rB = contactPoint - other!.Transform.WorldPosition;
            contactVelocityB = other.Velocity + Vector3.Cross(other.AngularVelocity, rB);
        }

        Vector3 relativeVelocity = contactVelocityA - contactVelocityB;
        float velocityNormalLength = Vector3.Dot(relativeVelocity, normal);

        float penetration = resolution.Length;
        const float penetrationThreshold = 0.005f;
        float penetrationCorrectionPercent = 0.15f / PhysicsSystem.SolverIterations; 

        if (penetration > penetrationThreshold)
        {
            Vector3 positionalCorrection = penetrationCorrectionPercent * normal * (penetration - penetrationThreshold);
            GameObject.Transform.WorldPosition += positionalCorrection * massPercentA;
            if (otherHasPhysics)
                other!.Transform.WorldPosition -= positionalCorrection * massPercentB;
        }

        if (velocityNormalLength < 0f)
        {
            Vector3 linearCorrection = normal * velocityNormalLength;

            Vector3 impulseA = -linearCorrection * massPercentA;
            Vector3 impulseB = linearCorrection * massPercentB;

            AddLinearImpulse(impulseA);
            if (otherHasPhysics)
                other!.AddLinearImpulse(impulseB);

            if (!DisableRotation)
            {      
                Vector3 torqueA = Vector3.Cross(rA, impulseA * 0.25f);
                AddTorque(torqueA);
            }

            if (otherHasPhysics && !other!.DisableRotation)
            {
                Vector3 torqueB = Vector3.Cross(rB, impulseB * 0.25f);
                other!.AddTorque(torqueB);
            }
        }
    }

    public void AddLinearImpulse(Vector3 impulse, bool wake = false)
    {
        Velocity += impulse;
    }

    public void AddTorque(Vector3 torque, bool wake = false)
    {
        Vector3 size = col.OBBHalfExtents * 2f;
        float inertia =
        (
            Mass *
            (size.X * size.X +
            size.Y * size.Y +
            size.Z * size.Z)
        ) / 12f;

        Vector3 angularAcceleration = torque / inertia;

        AngularVelocity += angularAcceleration;
    }
}
