using CruxEngine.Physics;

namespace CruxEngine.Core;

public abstract class ColliderComponent : Component
{
    public (Vector3 MinKey, Vector3 MaxKey) OctreeKeys;

    public Vector3 SphereCenter;
    public float SphereRadius;

    public Vector3 AABBMin;
    public Vector3 AABBMax;

    public ColliderComponent(GameObject gameObject): base(gameObject)
    {
        PhysicsSystem.RegisterColliderObject(this);
    }

    public override void OnFrozenStateChanged(bool IsFrozen)
    {
        if(IsFrozen)
        {
            ComputeBounds();
            OctreeKeys = PhysicsSystem.Tree.RegisterComponentGetAABB(this, AABBMin, AABBMax);
        }else
        {
            PhysicsSystem.Tree.UnregisterComponent(this, OctreeKeys);
        }
    }

    public override void OnDelete()
    {
        PhysicsSystem.UnregisterColliderObject(this);
        if(GameObject.IsFrozen)
            PhysicsSystem.Tree.UnregisterComponent(this, OctreeKeys);
    }

    public abstract void ComputeBounds();

    public abstract List<Vector3> GetWorldPoints();
    public abstract List<Vector3> GetWorldNormals();
    public abstract List<Vector3> GetWorldEdges();
}

