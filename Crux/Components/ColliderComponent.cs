using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Physics;

namespace Crux.Components;

public class ColliderComponent : RenderComponent
{     
    readonly MeshComponent mesh;

    static int boundsVao = -1;
    public static Shader boundsMaterial = null!;

    //World Space
    public Vector3 SphereCenter;
    public float SphereRadius;

    //World Space
    public Vector3 AABBMin;
    public Vector3 AABBMax;

    //World Space
    public Vector3 OBBCenter; 
    public Vector3[] OBBAxes = [];
    public Vector3 OBBHalfExtents;

    public Color4 AABBColor = Color4.Orange;
    public Color4 OBBColor = Color4.Blue;

    public bool ShowColliders = false;

    public int ColliderIndex = -1;

    public (Vector3 MinKey, Vector3 MaxKey) OctreeKeys;

    public ColliderComponent(GameObject gameObject): base(gameObject)
    {
        mesh = GetComponent<MeshComponent>();
        
        if(boundsVao == -1)
        {
            boundsMaterial = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Outline);
            
            boundsVao = GraphicsCache.GetLineVAO("LineBounds", Shapes.LineBounds);
        }

        ComputeBounds();
        PhysicsSystem.RegisterAsStatic(this);
    }

    public override void Delete(bool OnlyRemovingComponent)
    {
        PhysicsSystem.UnregisterAsStatic(this);
    }
    
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        ColliderComponent clone = new ColliderComponent(gameObject);

        return clone;
    }
    
    public void ComputeBounds()
    {
        if(GameObject.Stationary)
            return;

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

    public override void Render()
    { 
        //ShowColliders = true;

        if(!ShowColliders)
            return;

        if(OBBAxes == null)
            return;
        //ComputeBounds();
        
        //AABB
        Vector3 middle = (AABBMin + AABBMax) * 0.5f;
        Vector3 size = AABBMax - AABBMin;
        Matrix4 BoundsModelMatrix = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(middle);

        boundsMaterial.SetUniform("model", BoundsModelMatrix);
        boundsMaterial.TextureHue = AABBColor;
        boundsMaterial.Bind();
        GL.BindVertexArray(boundsVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, Shapes.LineBounds.Count);
        GraphicsCache.DrawCallsThisFrame++;
        GL.BindVertexArray(0);
        boundsMaterial.Unbind();
        
        
        //OBB
        Matrix4 scaleMatrix = Matrix4.CreateScale(OBBHalfExtents * 2.0f);
        Matrix4 rotationMatrix = MatrixHelper.CreateRotationMatrixFromAxes(OBBAxes);
        Matrix4 translationMatrix = Matrix4.CreateTranslation(OBBCenter);
        BoundsModelMatrix = scaleMatrix * rotationMatrix * translationMatrix;
        
        boundsMaterial.SetUniform("model", BoundsModelMatrix);
        boundsMaterial.TextureHue = OBBColor;
        boundsMaterial.Bind();
        GL.BindVertexArray(boundsVao);
        GL.DrawArrays(PrimitiveType.Lines, 0, Shapes.LineBounds.Count);
        GraphicsCache.DrawCallsThisFrame++;
        GL.BindVertexArray(0);
        boundsMaterial.Unbind();
        
        
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


    public List<Vector3> GetWorldPoints()
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

    public List<Vector3> GetWorldNormals()
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
    
    public List<Vector3> GetWorldEdges()
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

    public override void RegisterAsStationary()
    {
        throw new NotImplementedException();
    }
}
