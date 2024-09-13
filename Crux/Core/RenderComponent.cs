using Crux.Physics;

namespace Crux.Core;

public abstract class RenderComponent : Component
{
    public bool Hidden = false;

    public OctreeNode StationaryVisibilityNode = null!;

    public RenderComponent(GameObject gameObject): base(gameObject)
    {
        GameObject = gameObject;
    }

    public abstract void Render();

    public abstract void RegisterAsStationary();
}

