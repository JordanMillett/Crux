using System.Globalization;
using Crux.Components;

namespace Crux.Core;

public class GameObject
{
    public string Name;
    
    public event Action<bool> OnFrozenStateChanged;
    private bool isFrozen = false;
    public bool IsFrozen 
    { 
        get => isFrozen;
        private set 
        { 
            if (isFrozen != value)
            {
                isFrozen = value;
                OnFrozenStateChanged?.Invoke(isFrozen);
            }
        }
    }

    public void Freeze() => IsFrozen = true;
    public void Unfreeze() => IsFrozen = false;

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

    /// <summary>
    /// Returns a specified concrete type if it is attached to the GameObject. A concrete type or an abstract type can be passed.
    /// </summary>
    /// <remarks>
    /// Null will be returned if the GameObject does not contain a concrete type that matches either
    /// the specified concrete type or a child of the specified abstract type. 
    /// </remarks>
    public T? GetComponent<T>() where T : Component
    {
        if (!HasComponent<T>())
        {
            Logger.LogWarning($"Cannot get '{typeof(T).Name}' as GameObject '{Name}' does not contain '{typeof(T).Name}'.");
            return null;
        }

        Type checkType = typeof(T);

        if (checkType.IsAbstract)
        {
            if (checkType == typeof(Component))
            {
                return components.Values.FirstOrDefault() as T;
            }
            else
            {
                foreach (var pair in components)
                {
                    if (checkType.IsAssignableFrom(pair.Key))
                    {
                        return pair.Value as T;
                    }
                }
            }
        }
        else
        {
            if (components.TryGetValue(checkType, out var component))
            {
                return component as T;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds a specified concrete type as a new component that is attached to the GameObject.
    /// </summary>
    /// <returns>The newly created component, the existing component of the same concrete type, or null if adding the component failed.</returns>
    /// <remarks>Null will be returned if the GameObject contains a conflicting component.</remarks>
    public T? AddComponent<T>() where T : Component
    {
        if(typeof(T).IsAbstract)
        {
            Logger.LogWarning($"Cannot add '{typeof(T).Name}' to GameObject '{Name}' as the type is abstract.");
            return null;
        }

        if(HasComponent<T>()) //Return existing component if possible
        {
            T found = GetComponent<T>()!;
            Logger.LogWarning($"Cannot add '{typeof(T).Name}' as GameObject '{Name}' already contains '{found.GetType().Name}'.");
            return found;
        }

        if(HasComponentOrSibling<T>()) //Return null if component conflicts
        {
            Logger.LogWarning($"Cannot add '{typeof(T).Name}' as GameObject '{Name}' already contains another '{typeof(T).BaseType!.Name}'.");
            return null;
        }

        T component = (T)Activator.CreateInstance(typeof(T), this)!;
        components[typeof(T)] = component;

        return component;
    }

    /// <summary>
    /// Removes a specified concrete type that is attached to the GameObject.
    /// </summary>
    /// <remarks>Nothing will be removed is the specified type is abstract or no concrete matches are found.</remarks>
    public void RemoveComponent<T>() where T : Component
    {
        if(typeof(T).IsAbstract)
        {
            Logger.LogWarning($"Cannot remove '{typeof(T).Name}' from GameObject '{Name}' as the type is abstract.");
            return;
        }

        if(!HasComponent<T>())
        {
            Logger.LogWarning($"Cannot remove '{typeof(T).Name}' as GameObject '{Name}' does not contain '{typeof(T).Name}'.");
            return;
        }

        components[typeof(T)].Delete();
        components.Remove(typeof(T));
    }
    
    /// <summary>
    /// Returns true if the GameObject contains a concrete component that
    /// matches either the specified concrete type, or a child of the specified abstract type.
    /// </summary>
    /// <remarks>If the specified abstract type is 'Component' then true will be returned if any concrete component exist.</remarks>
    public bool HasComponent<T>() where T : Component
    {
        Type checkType = typeof(T);

        if(checkType.IsAbstract)
        {
            if(checkType == typeof(Component)) //Return true if any components exist
                return components.Count > 0;
            else
                return components.Keys.Any(checkType.IsAssignableFrom);  //Return true if any children of the abstract type exist
        }else
        {
            return components.Keys.Any(stored => checkType == stored); //find exact type matches
        }
    }

    /// <summary>
    /// Returns true if the GameObject contains a concrete component that
    /// matches either the specified concrete type, a sibling of the specified concrete type, or a child of the specified abstract type.
    /// </summary>
    /// <remarks>If the specified abstract type is 'Component' then true will be returned if any concrete component exist.</remarks>
    public bool HasComponentOrSibling<T>() where T : Component
    {
        if(HasComponent<T>())
            return true;
        
        Type checkType = typeof(T);
        Type? parentType = checkType.BaseType;

        if (parentType != null && parentType != typeof(Component) && parentType.IsAbstract)
            return components.Keys.Any(parentType.IsAssignableFrom);
        
        return false;
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
