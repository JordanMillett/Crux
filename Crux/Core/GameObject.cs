using System.Globalization;
using Crux.Components;

namespace Crux.Core;

public class GameObject
{
    public string Name;

    public bool Stationary = false;

    private static readonly Dictionary<Type, Component> defaultComponents = [];

    // ========== Private Fields ==========
    private Dictionary<Type, Component> components = new Dictionary<Type, Component>();
    private readonly TransformComponent transform = null!;
    
    // ========== Public Properties ==========
    public TransformComponent Transform
    {
        get
        {
            return transform;
        }
    }
    
    // ========== Public Constructors ==========
    public GameObject(string name)
    {
        transform = AddComponent<TransformComponent>();
        GameEngine.Link.OnUpdateCallback += Update;

        Name = name;
    }
    
    // ========== Public Instance Methods ==========
    public GameObject Clone()
    {
        GameObject cloned = new GameObject(Name + " copy")
        {
            components = []
        };

        foreach (var pair in this.components)
        {
            var componentType = pair.Key;
            var component = pair.Value;
            
            cloned.components[componentType] = component.Clone(cloned);
        }

        return cloned;
    }

    public void Delete()
    {
        GameEngine.Link.Instantiated.Remove(this);

        foreach (var pair in components)
        {
            components[pair.Key].Delete(false);
        }
    }

    public void RemoveComponent<T>() where T : Component
    {
        if(!HasComponent<T>())
        {
            Logger.LogWarning($"GameObject '{Name}' doesn't contain '{typeof(T).Name}'.");
            return;
        }

        components[typeof(T)].Delete();
        components.Remove(typeof(T));
    }

    public T AddComponent<T>() where T : Component
    {
        if(HasComponent<T>())
        {
            Logger.LogWarning($"GameObject '{Name}' already contains '{typeof(T).Name}'.");
            return GetComponent<T>();
        }

        T component = (T)Activator.CreateInstance(typeof(T), this)!;
        components[typeof(T)] = component;

        return component;
    }

    public T? GetComponent<T>() where T : Component
    {
        if (components.TryGetValue(typeof(T), out Component? component))
        {
            return component as T ?? throw new InvalidOperationException($"Unable to cast '{typeof(T).Name}' to '{component.GetType().Name}' on GameObject '{Name}'.");
        }

        throw new KeyNotFoundException($"'{typeof(T).Name}' not found on GameObject '{Name}'.");
    }

    public List<T>? GetComponents<T>() where T : Component //used to get multiple render components
    {
        List<T> result = new List<T>();

        foreach (var component in components.Values)
        {
            if (component is T typedComponent)
            {
                result.Add(typedComponent);
            }
        }

        return result;
    }

    public bool HasComponent<T>() where T : Component
    {
        return components.ContainsKey(typeof(T));
    }

    public void Update()
    {
        foreach (Component component in components.Values)
        {
            component.Update();
        }
    }

    // ========== Public Override Methods ==========
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Components:");
        foreach (var component in components.Values)
        {
            sb.Append($"{component.ToString()}");
        }

        return sb.ToString();
    }
}
