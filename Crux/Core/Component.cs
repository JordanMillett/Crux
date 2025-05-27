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

    public virtual void Update() {}

    public virtual void Delete() {}
}

