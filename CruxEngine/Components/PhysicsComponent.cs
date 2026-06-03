using CruxEngine.Physics;
using CruxEngine.Utilities.Helpers;
using CruxEngine.Utilities.IO;

namespace CruxEngine.Components;

public class PhysicsMaterialJsonPreset : JsonPreset
{
    public float Friction { get; init; } = 0.35f;
    public float Bounciness { get; init; } = 0.00f;
}

public class PhysicsComponent : Component
{     
    public Vector3 Velocity = Vector3.Zero;
    public Vector3 AngularVelocity = Vector3.Zero;
    public float LinearDrag = 0.5f;
    public float AngularDrag = 1.0f;
    public float Mass = 1f;
    public float InverseMass => 1f/ Mass;

    public float Friction = 0.35f;
    public float Bouncines = 0.00f;
    
    public bool DisableRotation = false;

    public event Action<bool>? OnSleepStateChanged;
    private bool isSleeping = false;
    public bool IsSleeping 
    { 
        get => isSleeping;
        private set 
        { 
            if (isSleeping != value)
            {
                isSleeping = value;
                OnSleepStateChanged?.Invoke(isSleeping);
            }
        }
    }

    private float SleepTimer = 0f;
    public float SleepVelocityThreshold = 0.4f; //0,5f
    public float SleepAngularThreshold = 0.4f; //0.5f
    public float SleepTime = 1.0f; //0.5f

    private readonly ColliderComponent col;
    
    public PhysicsComponent(GameObject gameObject): base(gameObject)
    {
        col = GetComponent<ColliderComponent>();
        PhysicsSystem.RegisterPhysicsObject(col, this);
        //this.Transform.Changed += Wake;
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

    public void SleepCheck()
    {
        //Sleeping

        float linearSpeedSq = Velocity.LengthSquared;
        float angularSpeedSq = AngularVelocity.LengthSquared;
        bool still =
            linearSpeedSq < SleepVelocityThreshold * SleepVelocityThreshold &&
            angularSpeedSq < SleepAngularThreshold * SleepAngularThreshold;

        //Logger.Log($"{linearSpeedSq} and {angularSpeedSq}, still? {still} for {SleepTimer}");

        if (still)
        {
            SleepTimer += Crux.Engine.fixedDeltaTime;

            if (SleepTimer >= SleepTime)
            {
                IsSleeping = true;
                Velocity = Vector3.Zero;
                AngularVelocity = Vector3.Zero;
                //Logger.LogWarning("SLEEP");
            }
        }
        else
        {
            SleepTimer = 0f;
            if(isSleeping)
                Wake();
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
        if (velocityNormalLength < 0f)
        {   
            float inertiaA = GetInertia();
            float inertiaB = float.MaxValue;
            if (otherHasPhysics)
                inertiaB = other!.GetInertia();

            float rotationalA = Vector3.Cross(rA, normal).LengthSquared / inertiaA;
            float rotationalB = 0f;
            if (otherHasPhysics)
                rotationalB = Vector3.Cross(rB, normal).LengthSquared / inertiaB;

            float effectiveMass = InverseMass + (otherHasPhysics ? other!.InverseMass : 0f) + rotationalA + rotationalB;

            if (effectiveMass > 0f)
            {
                float restitution = Bouncines; // start with zero bounce
                float j = -(1f + restitution) * velocityNormalLength / effectiveMass;

                float WakeImpulseThreshold = 0.2f;
                if (j > WakeImpulseThreshold)
                {
                    Wake();

                    if (otherHasPhysics)
                        other!.Wake();
                }

                Vector3 impulse = normal * j;

                //Linear
                Velocity += impulse * InverseMass;
                if (otherHasPhysics)
                    other!.Velocity -= impulse * other.InverseMass;

                //Angular
                if (!DisableRotation)
                    AngularVelocity += Vector3.Cross(rA, impulse) / inertiaA;

                if (otherHasPhysics && !other!.DisableRotation)
                    other!.AngularVelocity -= Vector3.Cross(rB, impulse) / inertiaB;

                //Friction

                Vector3 tangent =
                    relativeVelocity -
                    normal * velocityNormalLength;

                if (tangent.LengthSquared > 1e-8f)
                {
                    tangent = Vector3.Normalize(tangent);

                    float jt =
                        -Vector3.Dot(relativeVelocity, tangent) /
                        effectiveMass;

                    float frictionCoefficient = Friction; //0.25

                    float maxFrictionImpulse =
                        frictionCoefficient * j;

                    jt = Math.Clamp(
                        jt,
                        -maxFrictionImpulse,
                        maxFrictionImpulse);

                    Vector3 frictionImpulse =
                        tangent * jt;

                    Velocity += frictionImpulse * InverseMass;

                    if (otherHasPhysics)
                        other!.Velocity -= frictionImpulse * other.InverseMass;

                    if (!DisableRotation)
                        AngularVelocity +=
                            Vector3.Cross(rA, frictionImpulse) / inertiaA;

                    if (otherHasPhysics && !other!.DisableRotation)
                        other!.AngularVelocity -=
                            Vector3.Cross(rB, frictionImpulse) / inertiaB;
                }
            }
        }

        float penetration = resolution.Length;
        const float penetrationThreshold = 0.005f; //0.005f
        float penetrationCorrectionPercent = 0.50f / PhysicsSystem.SolverIterations;  //0.15
        if (penetration > penetrationThreshold)
        {
            Vector3 positionalCorrection = penetrationCorrectionPercent * normal * (penetration - penetrationThreshold);
            GameObject.Transform.WorldPosition += positionalCorrection * massPercentA;
            if (otherHasPhysics)
                other!.Transform.WorldPosition -= positionalCorrection * massPercentB;
        }
    }

    public float GetInertia()
    {
        Vector3 size = col.OBBHalfExtents * 2f;

        return
        (
            Mass *
            (
                size.X * size.X +
                size.Y * size.Y +
                size.Z * size.Z
            )
        ) / 12f;
    }

    public void Wake()
    {
        //Logger.Log("WOKEN");

        IsSleeping = false;
        SleepTimer = 0f;
    }

    public void AddLinearImpulse(Vector3 impulse, bool wake = false)
    {
        Wake();
        Velocity += impulse;
    }

    public void AddTorque(Vector3 torque, bool wake = false)
    {
        /*
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
        */
    }
}
