/*
using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class BoundsRenderComponent : RenderComponent
{
    public static MeshBuffer meshBuffer;
    public static Shader shader { get; set; } = null!;

    public static Dictionary<MeshBuffer, BoundsRenderComponent> Connected = [];

    public MeshBoundsColliderComponent Source;
    public 

    //Instanced Data
    public Color4 AABBColor = Color4.Orange;
    public Color4 OBBColor = Color4.Blue;

    public BoundsRenderComponent(GameObject gameObject): base(gameObject)
    {
        if(shader == null)
            shader = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Outline);
        if(meshBuffer == null)
            meshBuffer = GraphicsCache.GetLineBuffer("LineBounds", Shapes.LineBounds);

        Connected.Add(meshBuffer, this);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }

    public override Component Clone(GameObject gameObject)
    {
        BoundsRenderComponent clone = new BoundsRenderComponent(gameObject);

        return clone;
    }

    public override void Render()
    {
        shader.SetUniform("model", GameObject.Transform.ModelMatrix);

        shader.TextureHue = Color;
        
        shader.Bind();
        
        GL.BindVertexArray(meshBuffer.VAO);

        GL.DrawArrays(PrimitiveType.Lines, 0, Shapes.LineAnchor.Length);

        GL.BindVertexArray(0);

        shader.Unbind();
    }

    public override void Render()
    { 
        ComputeBounds();

        if(OBBAxes == null)
            return;
        
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
}
*/
/*
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
}
*/