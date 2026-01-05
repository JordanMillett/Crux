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
            Quaternion deltaRot = Quaternion.FromAxisAngle(Vector3.Normalize(AngularVelocity), AngularVelocity.Length * Crux.Engine.fixedDeltaTime);
            GameObject.Transform.WorldRotation = deltaRot * GameObject.Transform.WorldRotation;
            AngularVelocity *= MathF.Exp(-AngularDrag * Crux.Engine.fixedDeltaTime);
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
        float penetration = resolution.Length;

        //ignore intersections under this value
        const float penetrationThreshold = 0.005f;                //Lower values cause jitter
        //percentage of penetration position to correct per frame (pos offset)
         float penetrationCorrectionPercent = 0.15f / PhysicsSystem.SolverIterations;        //Higher values can overshoot and cause bouncing, lower are too smooth
        //percentage of penetration velocity to correct per frame (velocity push)
        //const float velocityCorrectionPercent = 0.15f;           //Higher values can overshoot and cause bouncing, lower reduces bouncing but slower settling
        //clamp on max penetration velocity to correct per frame
        //const float velocityCorrectionLimit = 1.00f;             //Can overshoot heavily if too high, slower corrections for deep penetrations if low

        if (penetration > penetrationThreshold)
        {
            //Penetration-based position correction
            Vector3 positionalCorrection = penetrationCorrectionPercent * normal * (penetration - penetrationThreshold);
            GameObject.Transform.WorldPosition += positionalCorrection * massPercentA;
            if (otherHasPhysics)
                other!.Transform.WorldPosition -= positionalCorrection * massPercentB;

            //Penetration-based velocity correction
            /*
            Vector3 velocityCorrection = velocityCorrectionPercent * (penetration - penetrationThreshold) * normal / Crux.Engine.fixedDeltaTime;

            if (velocityCorrection.Length > velocityCorrectionLimit)
                velocityCorrection = Vector3.Normalize(velocityCorrection) * velocityCorrectionLimit;

            Velocity -= velocityCorrection * massPercentA;
            if (otherHasPhysics)
                other!.Velocity += velocityCorrection * massPercentB;
            */

            Vector3 relativeVelocity = Velocity - (otherHasPhysics ? other!.Velocity : Vector3.Zero);
            float velocityNormal = Vector3.Dot(relativeVelocity, normal);

            if (velocityNormal < 0f)
            {       
                Vector3 linearCorrection = normal * velocityNormal;

                // Apply linear velocity
                AddLinearImpulse(-linearCorrection * massPercentA);
                if (otherHasPhysics)
                    other!.AddLinearImpulse(linearCorrection * massPercentB);

                // Compute torque / angular velocity
                if (!DisableRotation)
                {
                    Vector3 localContactPoint = contactPoint - GameObject.Transform.WorldPosition;
                    Vector3 torque = Vector3.Cross(localContactPoint, -linearCorrection * massPercentA);
                    AddAngularImpulse(torque / Mass);
                }

                if (otherHasPhysics && !other!.DisableRotation)
                {
                    Vector3 otherLocalContactPoint = otherHasPhysics ? contactPoint - other!.Transform.WorldPosition : Vector3.Zero;
                    Vector3 otherTorque = Vector3.Cross(otherLocalContactPoint, linearCorrection * massPercentB);
                    other!.AddAngularImpulse(otherTorque / other.Mass);
                }
            }
        }
    }

    public void AddLinearImpulse(Vector3 impulse, bool wake = false)
    {
        Velocity += impulse;
    }

    public void AddAngularImpulse(Vector3 impulse, bool wake = false)
    {
        AngularVelocity += impulse;
    }
}
