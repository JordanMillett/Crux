using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Physics;

namespace Crux.Components;

public class MeshBoundsColliderComponent : ColliderComponent
{     
    readonly MeshComponent mesh;

    //World Space
    public Vector3 OBBCenter; 
    public Vector3[] OBBAxes = [];
    public Vector3 OBBHalfExtents;

    public int ColliderIndex = -1;

    public MeshBoundsColliderComponent(GameObject gameObject): base(gameObject)
    {
        mesh = GetComponent<MeshComponent>();
        ComputeBounds();

        /*
        GameObject bounds = GameEngine.Link.InstantiateGameObject();
        bounds.Transform.Parent = this.Transform;
        bounds.Transform.LocalPosition = Vector3.Zero;
        bounds.AddComponent<BoundsRenderComponent>().Source = this;
        */
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
    
    public override void ComputeBounds()
    {
        if(ColliderIndex > -1 && ColliderIndex < mesh.data.Submeshes.Count)
        {
            (AABBMin, AABBMax) = mesh.data.Submeshes[ColliderIndex].GetWorldSpaceAABB(GameObject.Transform.ModelMatrix);
            (OBBCenter, OBBAxes, OBBHalfExtents) = mesh.data.Submeshes[ColliderIndex].GetWorldSpaceOBB(GameObject.Transform.ModelMatrix);
        }else
        {
            (AABBMin, AABBMax) = mesh.data.GetWorldSpaceAABB(GameObject.Transform.ModelMatrix);
            (OBBCenter, OBBAxes, OBBHalfExtents) = mesh.data.GetWorldSpaceOBB(GameObject.Transform.ModelMatrix);
        }

        SphereCenter = (AABBMin + AABBMax) * 0.5f;
        SphereRadius = ((AABBMax - AABBMin) * 0.5f).Length;
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

    public override List<Vector3> GetWorldPoints()
    {
        List<Vector3> points = new List<Vector3>();

        // Iterate through all 8 combinations of half-extents applied to the OBB axes
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = OBBCenter
                        + (OBBAxes[0] * OBBHalfExtents.X * x)
                        + (OBBAxes[1] * OBBHalfExtents.Y * y)
                        + (OBBAxes[2] * OBBHalfExtents.Z * z);

                    corner = GameObject.Transform.WorldRotation * (corner - OBBCenter) + OBBCenter;
                    corner = GameObject.Transform.WorldRotation * (corner - OBBCenter) + OBBCenter;
                    points.Add(corner);
                }
            }
        }

        return points;
    }

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
        List<Vector3> vertices = GetWorldPoints();
        List<Vector3> edges = new List<Vector3>();

        int[,] edgePairs = new int[,]
        {
            { 0, 1 }, { 1, 3 }, { 3, 2 }, { 2, 0 }, 
            { 4, 5 }, { 5, 7 }, { 7, 6 }, { 6, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
        };

        for (int i = 0; i < edgePairs.GetLength(0); i++)
        {
            Vector3 edge = vertices[edgePairs[i, 1]] - vertices[edgePairs[i, 0]];
            edges.Add(edge);
        }

        return edges;
    }
}
