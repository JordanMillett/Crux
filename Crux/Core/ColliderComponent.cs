using Crux.Physics;

namespace Crux.Core;

public abstract class ColliderComponent : Component
{
    public (Vector3 MinKey, Vector3 MaxKey) OctreeKeys;

    public Vector3 SphereCenter;
    public float SphereRadius;

    public Vector3 AABBMin;
    public Vector3 AABBMax;

    public ColliderComponent(GameObject gameObject): base(gameObject)
    {
        GameObject = gameObject;
    }

    public override void HandleFrozenStateChanged(bool frozen)
    {
        if(frozen)
        {
            ComputeBounds();
            PhysicsSystem.RegisterAsStatic(this);
        }else
        {
            PhysicsSystem.UnregisterAsStatic(this);
        }
    }

    public override void Delete(bool OnlyRemovingComponent)
    {
        if(GameObject.IsFrozen)
            PhysicsSystem.UnregisterAsStatic(this);
    }

    public abstract void ComputeBounds();

    public abstract List<Vector3> GetWorldPoints();
    public abstract List<Vector3> GetWorldNormals();
    public abstract List<Vector3> GetWorldEdges();
}

