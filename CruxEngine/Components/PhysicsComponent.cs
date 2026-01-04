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
    //public float Restitution = 0.2f;
    //public float StaticFriction = 0.6f;
    //public float KineticFriction = 0.4f;
    //public float AngularStaticFriction = 0.1f; 
    //public float AngularKineticFriction = 0.1f;

    private float LastInteracted = 0f;
    private readonly float SleepTime = 4f;
    public bool Awake = true;

    public bool DisableRotation = false;

    private readonly float threshold = 0.01f * 0.01f;
    private readonly float requiredAwakeImpulse = 0.01f * 0.01f;

    private readonly ColliderComponent col;
    
    public PhysicsComponent(GameObject gameObject): base(gameObject)
    {
        col = GetComponent<ColliderComponent>();
        PhysicsSystem.RegisterPhysicsObject(col, this);
        
        LastInteracted = Crux.Engine.fixedTotalTime;
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
        //Freeze if sleeping
        if(!Awake)
        {
            Velocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            return;
        }

        //Integrate
        float Delta = Crux.Engine.fixedDeltaTime;

        Velocity += PhysicsSystem.Gravity * Delta;
        Velocity *= 1f - (LinearDrag * Delta);
        GameObject.Transform.WorldPosition += Velocity * Delta;
        
        //Freeze rotation for player
        if(DisableRotation)
        {
            AngularVelocity = Vector3.Zero;
        }else
        {
            Quaternion deltaRotation = Quaternion.FromEulerAngles(AngularVelocity * Delta);
            GameObject.Transform.WorldRotation = deltaRotation * GameObject.Transform.WorldRotation;
            AngularVelocity *= 1f - (AngularDrag * Delta);
        }
        
        if (Velocity.LengthSquared < threshold && AngularVelocity.LengthSquared < threshold)
        {
            if (Crux.Engine.fixedTotalTime > LastInteracted + SleepTime)
            {
                Velocity = Vector3.Zero;
                AngularVelocity = Vector3.Zero;
                Awake = false;
            }
        }
    }

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
        const float penetrationCorrectionPercent = 0.15f;        //Higher values can overshoot and cause bouncing, lower are too smooth
        //percentage of penetration velocity to correct per frame (velocity push)
        const float velocityCorrectionPercent = 0.15f;           //Higher values can overshoot and cause bouncing, lower reduces bouncing but slower settling
        //clamp on max penetration velocity to correct per frame
        const float velocityCorrectionLimit = 1.00f;             //Can overshoot heavily if too high, slower corrections for deep penetrations if low

        if (penetration > penetrationThreshold)
        {
            Vector3 positionalCorrection = penetrationCorrectionPercent * normal * (penetration - penetrationThreshold);
            GameObject.Transform.WorldPosition -= positionalCorrection * massPercentA;
            if (otherHasPhysics)
                other!.Transform.WorldPosition += positionalCorrection * massPercentB;

            Vector3 velocityCorrection = velocityCorrectionPercent * (penetration - penetrationThreshold) * normal / Crux.Engine.fixedDeltaTime;

            if (velocityCorrection.Length > velocityCorrectionLimit)
                velocityCorrection = Vector3.Normalize(velocityCorrection) * velocityCorrectionLimit;

            Velocity -= velocityCorrection * massPercentA;
            if (otherHasPhysics)
                other!.Velocity += velocityCorrection * massPercentB;
        }





        Vector3 relativeVelocity =
        Velocity - (otherHasPhysics ? other!.Velocity : Vector3.Zero);

        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        // Objects are separating → no impulse
        if (velAlongNormal > 0f)
            return;

        const float restitution = 0.0f; // bounciness

        float impulseScalar =
            -(1f + restitution) * velAlongNormal / totalInverseMass;

        Vector3 impulse = impulseScalar * normal;

        // ---- Angular velocity (THIS IS THE FIX)
        Vector3 rA = contactPoint - GameObject.Transform.WorldPosition;
        AngularVelocity -= Vector3.Cross(rA, impulse) * InverseMass;

        if (otherHasPhysics)
        {
            Vector3 rB = contactPoint - other!.Transform.WorldPosition;
            other!.AngularVelocity += Vector3.Cross(rB, impulse) * other.InverseMass;
        }

        /*
        if (otherHasPhysics)
        {
            Vector3 relativeVelocity = Velocity - other!.Velocity;
            Vector3 tangentVelocity = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;

            const float frictionCoefficient = 0.5f; // tweak this
            Vector3 frictionImpulse = tangentVelocity * frictionCoefficient;

            Velocity -= frictionImpulse * massPercentA;
            other!.Velocity += frictionImpulse * massPercentB;
        }
        else
        {
            Vector3 tangentVelocity = Velocity - Vector3.Dot(Velocity, normal) * normal;
            const float frictionCoefficient = 0.5f;
            Velocity -= tangentVelocity * frictionCoefficient;
        }*/
    }
    

    /*
    public void RespondToCollision(Vector3 contactPoint, Vector3 resolution, PhysicsComponent other)
    {      
        // ===== Position =====
        //Calculate mass
        bool otherIsStatic = other == null;
        float otherMass = otherIsStatic ? 0f : other!.Mass;
        float totalMass = Mass + otherMass;

        //Offset object
        float correctionStrength = otherIsStatic ? 1.0f : (otherMass / totalMass);
        correctionStrength *= otherIsStatic ? 1f : 0.5f; 
        float biasPercent = 0.1f; // small fraction of penetration
        float velocityBiasPercent = 0.2f; // tweak for strength
        float maxBiasVelocity = 1f;
        float slop = 0.005f;       // tiny tolerance
        if (resolution.Length > slop)
        {
            Vector3 biasCorrection = biasPercent * resolution.Normalized() * (resolution.Length - slop);
            GameObject.Transform.WorldPosition -= biasCorrection * correctionStrength;

            Vector3 normal = resolution.Normalized();
            Vector3 biasVelocity = velocityBiasPercent * (resolution.Length - slop) * normal / Crux.Engine.fixedDeltaTime;

            if (biasVelocity.Length > maxBiasVelocity)
                biasVelocity = Vector3.Normalize(biasVelocity) * maxBiasVelocity;

            // Scale for mass distribution
            if (!otherIsStatic)
                Velocity -= biasVelocity * correctionStrength;
            else
                Velocity -= biasVelocity;
        }

        // ===== Linear Velocity =====
        //Determine relative velocity
        Vector3 otherVelocity = otherIsStatic ? Vector3.Zero : other!.Velocity;
        Vector3 resolutionNormal = resolution.LengthSquared > 0 ? Vector3.Normalize(resolution) : Vector3.Zero;
        Vector3 relativeVelocity = Velocity - otherVelocity;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, resolutionNormal);
        //Leave early if velocity will self resolve
        if (velocityAlongNormal < 0)
            return;

        // ===== Friction Calculation =====

        //Calculate impulse strength
        float impulseScalar = -(1 + Restitution) * velocityAlongNormal;
        if (!otherIsStatic) 
            impulseScalar /= totalMass;

        //Calculate and appy impulse
        Vector3 linearImpulse  = impulseScalar * resolutionNormal;
        Vector3 tangentialVelocity = relativeVelocity - velocityAlongNormal * resolutionNormal;
        if (tangentialVelocity.LengthSquared > 0)
        {
            Vector3 frictionDirection = Vector3.Normalize(tangentialVelocity);
            float maxStaticFriction = StaticFriction * Math.Abs(impulseScalar);
            float maxKineticFriction = KineticFriction * Math.Abs(impulseScalar);
            Vector3 frictionForce;

            if (velocityAlongNormal == 0)
            {
                // Static friction: prevent motion
                float frictionForceMagnitude = Math.Min(maxStaticFriction, tangentialVelocity.Length);
                frictionForce = -frictionDirection * frictionForceMagnitude;
            }else
            {
                // Kinetic friction: oppose motion
                float frictionForceMagnitude = Math.Min(maxKineticFriction, tangentialVelocity.Length);
                frictionForce = -frictionDirection * frictionForceMagnitude;
            }

            Vector3 totalLinearForce = linearImpulse + frictionForce;
            AddForce(totalLinearForce);
        }else
        {
            AddForce(linearImpulse);
        }

        // ===== Angular Velocity =====
        Vector3 relativePosition = contactPoint - GameObject.Transform.WorldPosition;
        Vector3 angularImpulse = Vector3.Cross(relativePosition, linearImpulse);

        // Calculate relative angular velocity
        Vector3 otherAngularVelocity = otherIsStatic ? Vector3.Zero : other!.AngularVelocity;
        Vector3 relativeAngularVelocity = AngularVelocity - otherAngularVelocity;

        // Project relative angular velocity onto the contact normal
        Vector3 angularVelocityAlongNormal = Vector3.Dot(relativeAngularVelocity, resolutionNormal) * resolutionNormal;
        Vector3 tangentialAngularVelocity = relativeAngularVelocity - angularVelocityAlongNormal;

        // ===== Angular Friction =====
        if (tangentialAngularVelocity.LengthSquared > 0)
        {
            Vector3 angularFrictionDirection = Vector3.Normalize(tangentialAngularVelocity);
            float maxAngularStaticFriction = AngularStaticFriction * Math.Abs(impulseScalar);
            float maxAngularKineticFriction = AngularKineticFriction * Math.Abs(impulseScalar);
            Vector3 angularFrictionTorque;

            if (velocityAlongNormal == 0)
            {
                // Static angular friction: prevent rotation
                float angularFrictionMagnitude = Math.Min(maxAngularStaticFriction, tangentialAngularVelocity.Length);
                angularFrictionTorque = -angularFrictionDirection * angularFrictionMagnitude;
            }
            else
            {
                // Kinetic angular friction: oppose rotation
                float angularFrictionMagnitude = Math.Min(maxAngularKineticFriction, tangentialAngularVelocity.Length);
                angularFrictionTorque = -angularFrictionDirection * angularFrictionMagnitude;
            }

            // Combine angular impulse and angular friction torque
            angularImpulse += angularFrictionTorque;
        }

        // Apply combined angular impulse and friction torque
        float inverseInertia = 1.0f / (2.0f * Mass); // Replace with actual inertia if available
        AddTorque(angularImpulse * inverseInertia);
    }
    */

    public void AddForce(Vector3 impulse, bool forceAwake = false)
    {
        if(impulse.LengthSquared < requiredAwakeImpulse)
            return;

        Velocity += impulse;
        if (impulse.LengthSquared > threshold || forceAwake)
        {
            LastInteracted = Crux.Engine.fixedTotalTime;
            Awake = true;
        }
    }

    public void AddTorque(Vector3 impulse, bool forceAwake = false)
    {
        if(DisableRotation)
            return;

        if(impulse.LengthSquared < requiredAwakeImpulse)
            return;

        AngularVelocity += impulse;
        if (impulse.LengthSquared > threshold || forceAwake)
        {
            LastInteracted = Crux.Engine.fixedTotalTime;
            Awake = true;
        }
    }
}
