using OpenTK.Graphics.OpenGL4;
using CruxEngine.Graphics;
using CruxEngine.Utilities.IO;
using CruxEngine.Utilities.Helpers;
using CruxEngine.Physics;

namespace CruxEngine.Components;

public class MeshBoundsColliderComponent : ColliderComponent
{     
    MeshComponent mesh;

    public MeshBoundsColliderComponent(GameObject gameObject): base(gameObject)
    {
        mesh = GetComponent<MeshComponent>();

        if(Debug.FlagEnabled("ShowMeshBounds"))
        {        
            GameObject bounds = Crux.Engine.InstantiateGameObject();
            bounds.Transform.Parent = this.Transform;
            bounds.Transform.LocalPosition = Vector3.Zero;
            bounds.AddComponent<BoundsRenderComponent>()!.Source = this;  //THIS NEEDS TO BE DELETED TO TO GARBAGE COLLECT
        }
    }
    
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        MeshBoundsColliderComponent clone = new MeshBoundsColliderComponent(gameObject);

        return clone;
    }
    
    public override void CalculateLocalBounds()
    {
        if(mesh == null)
            mesh = GetComponent<MeshComponent>();

        if(ColliderIndex > -1 && ColliderIndex < mesh.Data!.Submeshes.Count)
        {
            (LocalAABBMin, LocalAABBMax) = mesh.Data.Submeshes[ColliderIndex].GetLocalAABB();
            (LocalOBBCenter, LocalOBBAxes, LocalOBBHalfExtents) = mesh.Data.Submeshes[ColliderIndex].GetLocalOBB();
        }else
        {
            (LocalAABBMin, LocalAABBMax) = mesh.Data!.GetLocalAABB();
            (LocalOBBCenter, LocalOBBAxes, LocalOBBHalfExtents) = mesh.Data.GetLocalOBB();
        }
    }

    public override void CalculateWorldBounds()
    {
        (AABBMin, AABBMax) = GetWorldSpaceAABB();
        (OBBCenter, OBBAxes, OBBHalfExtents) = GetWorldSpaceOBB();

        SphereCenter = (AABBMin + AABBMax) * 0.5f;
        SphereRadius = ((AABBMax - AABBMin) * 0.5f).Length;
    }

    /*
    public override void CalculateWorldBounds()
    {
        if(ColliderIndex > -1 && ColliderIndex < mesh.Data!.Submeshes.Count)
        {
            (AABBMin, AABBMax) = mesh.Data.Submeshes[ColliderIndex].GetWorldSpaceAABB(GameObject.Transform.ModelMatrix);
            (OBBCenter, OBBAxes, OBBHalfExtents) = mesh.Data.Submeshes[ColliderIndex].GetWorldSpaceOBB(GameObject.Transform.ModelMatrix);
        }else
        {
            (AABBMin, AABBMax) = mesh.Data!.GetWorldSpaceAABB(GameObject.Transform.ModelMatrix);
            (OBBCenter, OBBAxes, OBBHalfExtents) = mesh.Data.GetWorldSpaceOBB(GameObject.Transform.ModelMatrix);
        }

        SphereCenter = (AABBMin + AABBMax) * 0.5f;
        SphereRadius = ((AABBMax - AABBMin) * 0.5f).Length;
    }
    */

    public (Vector3 center, Vector3[] axes, Vector3 halfExtents) GetWorldSpaceOBB()
    {
        MatrixHelper.Decompose(GameObject.Transform.ModelMatrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation);
    
        Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(rotation);
        Vector3[] worldAxes = new Vector3[3];
        for (int i = 0; i < 3; i++)
            worldAxes[i] = Vector3.Normalize(Vector3.TransformNormal(LocalOBBAxes[i], rotationMatrix));

        Vector3 worldHalfExtents = new Vector3(
            LocalOBBHalfExtents.X * scale.X,
            LocalOBBHalfExtents.Y * scale.Y,
            LocalOBBHalfExtents.Z * scale.Z
        );

        Vector3 worldCenter = Vector3.TransformPosition(LocalOBBCenter, GameObject.Transform.ModelMatrix);
        return (worldCenter, worldAxes, worldHalfExtents);
    }
    
    public (Vector3 min, Vector3 max) GetWorldSpaceAABB()
    {
        Vector3 worldMin = new Vector3(float.MaxValue);
        Vector3 worldMax = new Vector3(float.MinValue);

        foreach (Vector3 point in localPoints)
        {
            Vector3 worldPoint = Vector3.TransformPosition(point, GameObject.Transform.ModelMatrix);
            worldMin = Vector3.ComponentMin(worldMin, worldPoint);
            worldMax = Vector3.ComponentMax(worldMax, worldPoint);
        }

        return (worldMin, worldMax);
    }

    public Vector3 GetClosestPointOnOBB(Vector3 bestAxis, float overlapStart, float overlapEnd)
    {
        float localProjection = Vector3.Dot(OBBCenter, bestAxis);
        float clampedProjection = Math.Clamp(localProjection, overlapStart, overlapEnd);
        Vector3 closestLocalPoint = OBBCenter + (clampedProjection - localProjection) * bestAxis;
        return closestLocalPoint;
    }

    public Vector3 GetClosestPointOnOBB(Vector3 point)
    {
        Vector3 closestPoint = new Vector3(
            Math.Clamp(point.X, OBBCenter.X - OBBHalfExtents.X, OBBCenter.X + OBBHalfExtents.X),
            Math.Clamp(point.Y, OBBCenter.Y - OBBHalfExtents.Y, OBBCenter.Y + OBBHalfExtents.Y),
            Math.Clamp(point.Z, OBBCenter.Z - OBBHalfExtents.Z, OBBCenter.Z + OBBHalfExtents.Z)
        );

        return closestPoint;
    }

    public bool IsPointWithinOBB(Vector3 point)
    {
        bool withinBounds = 
            point.X >= (OBBCenter.X - OBBHalfExtents.X) && point.X <= (OBBCenter.X + OBBHalfExtents.X) &&
            point.Y >= (OBBCenter.Y - OBBHalfExtents.Y) && point.Y <= (OBBCenter.Y + OBBHalfExtents.Y) &&
            point.Z >= (OBBCenter.Z - OBBHalfExtents.Z) && point.Z <= (OBBCenter.Z + OBBHalfExtents.Z);

        return withinBounds;
    }

    public float DistanceFromOBB(Vector3 point)
    {
        // Project the point onto each axis of the OBB
        float distanceX = Math.Abs(Vector3.Dot(point - OBBCenter, OBBAxes[0])) - OBBHalfExtents.X;
        float distanceY = Math.Abs(Vector3.Dot(point - OBBCenter, OBBAxes[1])) - OBBHalfExtents.Y;
        float distanceZ = Math.Abs(Vector3.Dot(point - OBBCenter, OBBAxes[2])) - OBBHalfExtents.Z;

        // Calculate the distance for each axis (negative values indicate the point is inside the OBB)
        distanceX = Math.Max(0, distanceX); // If the point is inside the OBB, the distance is 0
        distanceY = Math.Max(0, distanceY);
        distanceZ = Math.Max(0, distanceZ);

        // Return the total distance as the sum of squared distances
        return (float)Math.Sqrt(distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ);
    }

    public override void CalculateLocalPoints()
    {
        int i = 0;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    localPoints[i++] =
                        LocalOBBCenter +
                        LocalOBBAxes[0] * LocalOBBHalfExtents.X * x +
                        LocalOBBAxes[1] * LocalOBBHalfExtents.Y * y +
                        LocalOBBAxes[2] * LocalOBBHalfExtents.Z * z;
                }
            }
        }
    }

    public override void CalculateWorldPoints()
    {
        for (int i = 0; i < 8; i++)
            worldPoints[i] = Vector3.TransformPosition(localPoints[i], GameObject.Transform.ModelMatrix);
            //worldPoints[i] = GameObject.Transform.WorldRotation * localPoints[i] + OBBCenter;
    }

    public override void CalculateLocalNormals()
    {
        if(ColliderIndex > -1 && ColliderIndex < mesh.Data!.Submeshes.Count)
            localNormals = mesh.Data.Submeshes[ColliderIndex].localNormals;
        else
            localNormals = mesh.Data!.localNormals;
    }

    public override void CalculateWorldNormals()
    {
        Matrix3 rotationScaleMatrix = MatrixHelper.ExtractRotationScale(GameObject.Transform.ModelMatrix);
        Matrix3 normalMatrix = MatrixHelper.Transpose(rotationScaleMatrix.Inverted());

        for(int i = 0; i < localNormals.Length; i++)
            worldNormals[i] = Vector3.Normalize(normalMatrix * localNormals[i]);
    }

    public override void CalculateWorldEdges()
    {
        for (int i = 0; i < edgePairs.GetLength(0); i++)
            worldEdges[i] = worldPoints[edgePairs[i, 1]] - worldPoints[edgePairs[i, 0]];
    }

    /*
    public override List<Vector3> GetWorldNormals()
    {
        List<Vector3> normals = new List<Vector3>();
        Vector3[] localNormals =
        {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        };

        Matrix3 rotationScaleMatrix = MatrixHelper.ExtractRotationScale(GameObject.Transform.ModelMatrix);
        Matrix3 normalMatrix = MatrixHelper.Transpose(rotationScaleMatrix.Inverted());

        foreach (var normal in localNormals)
        {
            // Transform using the normal matrix
            Vector3 worldNormal = Vector3.Normalize(normalMatrix * normal);
            normals.Add(worldNormal);
        }

        return normals;
    }
    
    public override List<Vector3> GetWorldEdges()
    {
        //List<Vector3> vertices = GetWorldPoints();
        List<Vector3> edges = new List<Vector3>();

        int[,] edgePairs = new int[,]
        {
            { 0, 1 }, { 1, 3 }, { 3, 2 }, { 2, 0 }, 
            { 4, 5 }, { 5, 7 }, { 7, 6 }, { 6, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
        };

        for (int i = 0; i < edgePairs.GetLength(0); i++)
        {
            Vector3 edge = worldPoints[edgePairs[i, 1]] - worldPoints[edgePairs[i, 0]];
            edges.Add(edge);
        }

        return edges;
    }
    */
}
