using Crux.Physics;

namespace Crux.Core;

public abstract class RenderComponent : Component
{
    public event Action<bool> OnHiddenStateChanged;
    private bool isHidden = false;
    public bool IsHidden 
    { 
        get => isHidden;
        private set 
        { 
            if (isHidden != value)
            {
                isHidden = value;
                OnHiddenStateChanged?.Invoke(isHidden);
            }
        }
    }

    public void Hide() => IsHidden = true;
    public void Unhide() => IsHidden = false;

    public OctreeNode ContainerNode = null!;

    public RenderComponent(GameObject gameObject): base(gameObject)
    {
        GameObject = gameObject;
    }

    public abstract void Render();
}

