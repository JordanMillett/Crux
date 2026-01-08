using CruxEngine.Physics;

namespace CruxEngine.Core;

public abstract class ColliderComponent : Component
{
    public (Vector3 MinKey, Vector3 MaxKey) OctreeKeys;

    //world space values
    public Vector3 SphereCenter;
    public float SphereRadius;
    
    //world space values
    public Vector3 AABBMin;
    public Vector3 AABBMax;

    //BROUGHT IN FROM MESH
    //world space values
    public Vector3 OBBCenter; 
    public Vector3[] OBBAxes = [];
    public Vector3 OBBHalfExtents;


    protected Vector3 LocalAABBMin;
    protected Vector3 LocalAABBMax;

    protected Vector3 LocalOBBCenter;
    protected Vector3[] LocalOBBAxes = new Vector3[3];
    protected Vector3 LocalOBBHalfExtents;

    //pre generted
    public ReadOnlySpan<Vector3> WorldPoints => worldPoints;
    public ReadOnlySpan<Vector3> WorldNormals => worldNormals;
    public ReadOnlySpan<Vector3> WorldEdges => worldEdges;

    protected readonly Vector3[] localPoints = new Vector3[8];
    protected readonly Vector3[] worldPoints = new Vector3[8];

    protected Vector3[] localNormals = new Vector3[6];
    protected Vector3[] worldNormals = new Vector3[6];

    protected Vector3[] worldEdges = new Vector3[12];

    public readonly int[,] edgePairs = new int[,]
    {
        { 0, 1 }, { 1, 3 }, { 3, 2 }, { 2, 0 }, 
        { 4, 5 }, { 5, 7 }, { 7, 6 }, { 6, 4 },
        { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
    };

    public int ColliderIndex = -1;

    public ColliderComponent(GameObject gameObject): base(gameObject)
    {
        PhysicsSystem.RegisterColliderObject(this);

        CalculateLocalData();
        CalculateWorldData();
    }

    public void CalculateLocalData()
    {
        CalculateLocalBounds();
        CalculateLocalPoints();
        CalculateLocalNormals();
    }

    public void CalculateWorldData()
    {
        CalculateWorldBounds();
        CalculateWorldPoints();
        CalculateWorldNormals();
        CalculateWorldEdges();
    }

    public abstract void CalculateLocalBounds();
    public abstract void CalculateLocalPoints();
    public abstract void CalculateLocalNormals();

    public abstract void CalculateWorldBounds();
    public abstract void CalculateWorldPoints();
    public abstract void CalculateWorldNormals();
    public abstract void CalculateWorldEdges();
    
    //public abstract List<Vector3> GetWorldPoints();
    //public abstract List<Vector3> GetWorldNormals();
    //public abstract List<Vector3> GetWorldEdges();

    public override void OnFrozenStateChanged(bool IsFrozen)
    {
        if(IsFrozen)
        {
            CalculateWorldBounds();
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
}

