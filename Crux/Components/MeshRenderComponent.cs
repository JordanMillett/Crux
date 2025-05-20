using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class MeshRenderComponent : RenderComponent
{
    private readonly MeshComponent mesh;

    public List<Shader> Shaders { get; set; } = new List<Shader>();
    public List<MeshBuffer> MeshBuffers { get; set; } = new List<MeshBuffer>();
    
    public MeshRenderComponent(GameObject gameObject): base(gameObject)
    {
        mesh = GetComponent<MeshComponent>();

        for(int i = 0; i < mesh.Data!.Submeshes.Count; i++)
        {
            MeshBuffers.Add(GraphicsCache.GetMeshBuffer(mesh.LoadedPath + "_" + i, mesh.Data.Submeshes[i]));
            Shaders.Add(AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Lit));
        }
    }

    public override void Delete()
    {
        for(int i = 0; i < mesh.Data!.Submeshes.Count; i++)
        {
            GraphicsCache.RemoveBuffer(mesh.LoadedPath + "_" + i);
        }
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        MeshRenderComponent clone = new MeshRenderComponent(gameObject);
        //clone.SetMaterial(meshMaterial.Clone());

        return clone;
    }
    
    public void SetShader(Shader passed, int index)
    {
        if(index >= Shaders.Count)
            return;
        
        Shaders[index]?.Delete();
        Shaders[index] = passed;
    }
    
    public void SetShaders(List<Shader> passed)
    {
        int least = Math.Min(Shaders.Count, passed.Count);

        for(int i = 0; i < least; i++)
        {
            Shaders[i]?.Delete();
            Shaders[i] = passed[i];
        }
    }

    public override void HandleFrozenStateChanged(bool IsFrozen)
    {
        if(IsFrozen)
        {
            (Vector3 AABBMin, Vector3 AABBMax) = mesh.Data!.GetWorldSpaceAABB(GameObject.Transform.ModelMatrix);
            ContainerNode  = GraphicsCache.Tree.RegisterComponentGetNode(this, AABBMin, AABBMax);
        }
    }
    
    public override void Render()
    {
        if(IsHidden || (this.GameObject.IsFrozen && ContainerNode!.Culled))
            return;

        for(int i = 0; i < MeshBuffers.Count; i++)
        {
            Shaders[i].SetUniform("model", this.Transform.ModelMatrix);
            Shaders[i].SetUniform("lightIndices", new int[4] {0, 1, 2, 3});
            Shaders[i].Bind();
        
            MeshBuffers[i].Draw(mesh.Data!.Submeshes[i].Indices.Length);

            Shaders[i].Unbind();
        }
    }
}
