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

    public abstract void ComputeBounds();

    public abstract List<Vector3> GetWorldPoints();
    public abstract List<Vector3> GetWorldNormals();
    public abstract List<Vector3> GetWorldEdges();
}

