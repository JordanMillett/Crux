using Crux.Components;

namespace Crux.Core;

public abstract class Component
{
    public GameObject GameObject { get; init; }

    public TransformComponent Transform
    {
        get
        {
            return GameObject.Transform;
        }
    }

    public T GetComponent<T>() where T : Component
    {
        return GameObject.GetComponent<T>()!;
    }

    public bool HasComponent<T>() where T : Component
    {
        return GameObject.HasComponent<T>();
    }
    
    public Component(GameObject gameObject)
    {
        GameObject = gameObject;
        GameObject.OnFrozenStateChanged += HandleFrozenStateChanged;
    }
    
    public abstract override string ToString();
    public abstract Component Clone(GameObject gameObject);

    public virtual void HandleFrozenStateChanged(bool IsFrozen) {}

    /// <summary>
    /// Returns true if the GameObject contains a concrete component that
    /// matches either the specified concrete type, a sibling of the specified concrete type, or a child of the specified abstract type.
    /// </summary>
    /// <remarks>If the specified abstract type is 'Component' then true will be returned if any concrete component exist.</remarks>
    public virtual void Update() {}
    
    /// <summary>
    /// Returns true if the GameObject contains a concrete component that
    /// matches either the specified concrete type, a sibling of the specified concrete type, or a child of the specified abstract type.
    /// </summary>
    /// <remarks>If the specified abstract type is 'Component' then true will be returned if any concrete component exist.</remarks>
    public virtual void Delete() {}

    ~Component()
    {
        if(Debug.FlagEnabled("LogFreedMemory"))
            Logger.LogWarning($"Component '{this.GetType().Name}' was freed from memory.");
    }
}

