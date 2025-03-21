using Crux.Physics;

namespace Crux.Components;

public class PhysicsComponent : Component
{     
    public Vector3 Velocity = Vector3.Zero;
    public Vector3 AngularVelocity = Vector3.Zero;
    public float LinearDrag = 0.5f; //1.4
    public float AngularDrag = 0.5f; //1.0
    public float Mass = 1f;
    public float Resitution = 0.2f;
    public float StaticFriction = 0.6f;
    public float KineticFriction = 0.4f;
    public float AngularStaticFriction = 0.1f; 
    public float AngularKineticFriction = 0.1f;

    float LastInteracted = 0f;
    float SleepTime = 2f;
    public bool Awake = true;

    public bool DisableRotation = false;

    float threshold = 0.5f * 0.5f;
    float requiredAwakeImpulse = 0.01f * 0.01f;

    ColliderComponent col;
    
    public PhysicsComponent(GameObject gameObject): base(gameObject)
    {
        col = GetComponent<ColliderComponent>();
        PhysicsSystem.RegisterAsPhysics(col, this);
        LastInteracted = GameEngine.Link.totalTime;
    }

    public override void Delete(bool OnlyRemovingComponent)
    {
        if(OnlyRemovingComponent)
        {
            PhysicsSystem.UnregisterAsPhysics(col, this);
            PhysicsSystem.RegisterAsStatic(col);
        }else
        {
            PhysicsSystem.UnregisterAsPhysics(col, this);
        }
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
        float Delta = GameEngine.Link.fixedDeltaTime;

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
        
        //Sleep Function 
        if (Velocity.LengthSquared < threshold && AngularVelocity.LengthSquared < threshold)
        {
            if (GameEngine.Link.totalTime > LastInteracted + SleepTime)
            {
                Velocity = Vector3.Zero;
                AngularVelocity = Vector3.Zero;
                Awake = false;
            }
        }
    }

    public void RespondToCollision(Vector3 contactPoint, Vector3 resolution, PhysicsComponent other)
    {      
        // ===== Position =====
        //Calculate mass
        bool otherIsStatic = other == null;
        float otherMass = otherIsStatic ? 0f : other.Mass;
        float totalMass = Mass + otherMass;

        //Offset object
        float correctionStrength = otherIsStatic ? 1.0f : (otherMass / totalMass);
        correctionStrength *= otherIsStatic ? 1f : 0.4f;
        Vector3 correction = resolution * correctionStrength;
        GameObject.Transform.WorldPosition -= correction;    

        // ===== Linear Velocity =====
        //Determine relative velocity
        Vector3 otherVelocity = otherIsStatic ? Vector3.Zero : other.Velocity;
        Vector3 resolutionNormal = resolution.LengthSquared > 0 ? Vector3.Normalize(resolution) : Vector3.Zero;
        Vector3 relativeVelocity = Velocity - otherVelocity;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, resolutionNormal);
        //Leave early if velocity will self resolve
        if (velocityAlongNormal < 0)
            return;

        // ===== Friction Calculation =====

        //Calculate impulse strength
        float impulseScalar = -(1 + Resitution) * velocityAlongNormal;
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
            Vector3 frictionForce = Vector3.Zero;

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
        Vector3 otherAngularVelocity = otherIsStatic ? Vector3.Zero : other.AngularVelocity;
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
            Vector3 angularFrictionTorque = Vector3.Zero;

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

    public void AddForce(Vector3 impulse, bool forceAwake = false)
    {
        if(impulse.LengthSquared < requiredAwakeImpulse)
            return;

        Velocity += impulse;
        if (impulse.LengthSquared > threshold || forceAwake)
        {
            LastInteracted = GameEngine.Link.totalTime;
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
            LastInteracted = GameEngine.Link.totalTime;
            Awake = true;
        }
    }
}
