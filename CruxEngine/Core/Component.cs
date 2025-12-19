using CruxEngine.Components;

namespace CruxEngine.Core;

public abstract class Component
{
    public GameObject GameObject { get; private set; }

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
        GameObject.OnFrozenStateChanged += OnFrozenStateChanged;
    }
    
    public abstract override string ToString();
    public abstract Component Clone(GameObject gameObject);

    public virtual void OnFrozenStateChanged(bool IsFrozen) {}

    public virtual void Update() {}
    
    public void Delete()
    {
        GameObject.OnFrozenStateChanged -= OnFrozenStateChanged;
        OnDelete();
    }

    public virtual void OnDelete() {} //make protected in the future, implement that more

    ~Component()
    {
        if(Debug.FlagEnabled("LogFreedMemory"))
            Logger.LogWarning($"Component '{this.GetType().Name}' was freed from memory.");
    }
}

