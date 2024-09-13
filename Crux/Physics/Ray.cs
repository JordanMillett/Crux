using Crux.Components;

namespace Crux.Physics;

public struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;
    public float Range;

    public Ray(Vector3 origin, Vector3 direction, float range = float.MaxValue)
    {
        Origin = origin;
        Direction = direction.Normalized();
        Range = range;
    }
}

public struct RayHit
{
    public ColliderComponent Collider;
    public float Distance;
    public Vector3 Point;

    public RayHit(ColliderComponent collider, float distance, Vector3 point)
    {
        Collider = collider;
        Distance = distance;
        Point = point;
    }
}
