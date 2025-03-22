namespace Crux.Components;

/// <summary>
/// Handles position, rotation, and scale data.
/// </summary>
/// <remarks>Frequently relays updates when data changes.</remarks>
public class TransformComponent : Component
{
    // ========== Private Fields ==========
    private TransformComponent parent = null!;
    private Vector3 worldPosition = Vector3.Zero;
    private Vector3 localPosition = Vector3.Zero;
    private Quaternion worldRotation = Quaternion.Identity;
    private Quaternion localRotation = Quaternion.Identity;
    private Vector3 scale = Vector3.One;

    // ========== Public Events ==========
    public Action? Changed;

    // ========== Public Properties ==========
    public TransformComponent Parent
    {
        get { return parent; }
        set
        {
            if (parent != value)
            {
                if (parent != null)
                    parent.Changed -= ParentChangedCallback;

                parent = value;

                if (parent != null)
                    parent.Changed += ParentChangedCallback;

                SyncWithParent();
                Changed?.Invoke();
            }
        }
    }

    public Vector3 WorldPosition
    {
        get { return worldPosition; }
        set
        {
            if(GameObject.IsFrozen)
            {
                Logger.LogWarning($"GameObject '{GameObject.Name}' WorldPosition cannot be set due to being frozen.");
                return;
            }

            worldPosition = value;
            if (parent != null)
                localPosition = Vector3.Transform(worldPosition - parent.worldPosition, Quaternion.Invert(parent.worldRotation));
            else
                localPosition = worldPosition;
            Changed?.Invoke();
        }
    }

    public Vector3 LocalPosition
    {
        get
        {
            if (parent != null)
                return Vector3.Transform(WorldPosition - parent.WorldPosition, Quaternion.Invert(parent.WorldRotation));
            return worldPosition;
        }
        set
        {
            if(GameObject.IsFrozen)
            {
                Logger.LogWarning($"GameObject '{GameObject.Name}' LocalPosition cannot be set due to being frozen.");
                return;
            }

            if (parent != null)
                worldPosition = parent.WorldPosition + Vector3.Transform(value, parent.WorldRotation);
            else
                worldPosition = value;

            localPosition = value;
            Changed?.Invoke();
        }
    }
    
    public Quaternion WorldRotation
    {
        get { return worldRotation; }
        set
        {
            if(GameObject.IsFrozen)
            {
                Logger.LogWarning($"GameObject '{GameObject.Name}' WorldRotation cannot be set due to being frozen.");
                return;
            }

            worldRotation = Quaternion.Normalize(value);
            if (parent != null)
                localRotation = Quaternion.Invert(parent.worldRotation) * worldRotation;
            else
                localRotation = worldRotation;
            Changed?.Invoke();
        }
    }

    public Quaternion LocalRotation
    {
        get
        {
            if (parent != null)
                return Quaternion.Invert(parent.WorldRotation) * worldRotation;
            return worldRotation;
        }
        set
        {
            if(GameObject.IsFrozen)
            {
                Logger.LogWarning($"GameObject '{GameObject.Name}' LocalRotation cannot be set due to being frozen.");
                return;
            }

            if (parent != null)
                worldRotation = parent.WorldRotation * value;
            else
                worldRotation = value;

            localRotation = value;
            Changed?.Invoke();
        }
    }

    public Vector3 Scale
    {
        get { return scale; }
        set
        {
            if(GameObject.IsFrozen)
            {
                Logger.LogWarning($"GameObject '{GameObject.Name}' Scale cannot be set due to being frozen.");
                return;
            }

            scale = value;
            Changed?.Invoke();
        }
    }

    public Matrix4 ModelMatrix
    {
        get
        {
            return Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(worldRotation) * Matrix4.CreateTranslation(WorldPosition);
        }
    }
    
    public Vector3 Forward
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, worldRotation));
        }
    }

    public Vector3 Up
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(Vector3.UnitY, worldRotation));
        }
    }

    public Vector3 Right
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(Vector3.UnitX, worldRotation));
        }
    }
    
    public Vector3 WorldEulerAngles
    {
        get
        {
            Vector3 euler = worldRotation.ToEulerAngles();
            Vector3 angles = new Vector3(
                MathHelper.RadiansToDegrees(euler.X),
                MathHelper.RadiansToDegrees(euler.Y),
                MathHelper.RadiansToDegrees(euler.Z)
            );

            return angles;
        }
        set
        {
            if(GameObject.IsFrozen)
            {
                Logger.LogWarning($"GameObject '{GameObject.Name}' WorldEulerAngles cannot be set due to being frozen.");
                return;
            }

            Vector3 radians = new Vector3(
                MathHelper.DegreesToRadians(value.X),
                MathHelper.DegreesToRadians(value.Y),
                MathHelper.DegreesToRadians(value.Z)
            );

            WorldRotation = Quaternion.FromEulerAngles(radians);
        }
    }

    public Vector3 LocalEulerAngles
    {
        get
        {
            Vector3 euler = localRotation.ToEulerAngles();
            Vector3 angles = new Vector3(
                MathHelper.RadiansToDegrees(euler.X),
                MathHelper.RadiansToDegrees(euler.Y),
                MathHelper.RadiansToDegrees(euler.Z)
            );

            return angles;
        }
        set
        {
            if(GameObject.IsFrozen)
            {
                Logger.LogWarning($"GameObject '{GameObject.Name}' LocalEulerAngles cannot be set due to being frozen.");
                return;
            }

            Vector3 radians = new Vector3(
                MathHelper.DegreesToRadians(value.X),
                MathHelper.DegreesToRadians(value.Y),
                MathHelper.DegreesToRadians(value.Z)
            );

            LocalRotation = Quaternion.FromEulerAngles(radians);
        }
    }

    // ========== Public Constructors ==========
    public TransformComponent(GameObject gameObject) : base(gameObject)
    {   

    }

    // ========== Public Override Methods ==========
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"{ this.GetType().Name }");
        sb.AppendLine($"- Position: { WorldPosition.ToString() }");
        sb.AppendLine($"- Rotation: { WorldEulerAngles.ToString() }");
        sb.AppendLine($"- Scale: { scale.ToString() }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new TransformComponent(gameObject)
        {
            WorldPosition = this.WorldPosition,
            WorldRotation = this.WorldRotation,
            Scale = this.Scale
        };
    }

    // ========== Private Instance Methods ========== 
    private void ParentChangedCallback()
    {
        SyncWithParent();
        Changed?.Invoke();
    }

    private void SyncWithParent()
    {
        if (parent != null)
        {
            worldPosition = parent.WorldPosition + Vector3.Transform(localPosition, parent.WorldRotation);
            worldRotation = parent.WorldRotation * localRotation;
        }
        else
        {
            worldPosition = localPosition;
            worldRotation = localRotation;
        }
    }
}
