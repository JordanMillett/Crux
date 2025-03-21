using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class MeshRenderComponent : RenderComponent
{
    MeshComponent mesh;

    public List<Shader> Shaders { get; set; } = new List<Shader>();
    public List<int> MeshVAOs { get; set; } = new List<int>();

    public List<string> VAOKeys = new List<string>();
    
    public MeshRenderComponent(GameObject gameObject): base(gameObject)
    {
        mesh = GetComponent<MeshComponent>();

        for(int i = 0; i < mesh.data.Submeshes.Count; i++)
        {
            MeshVAOs.Add(GraphicsCache.GetMeshVAO(mesh.path + "_" + i, mesh.data.Submeshes[i]));
            Shaders.Add(AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Lit));
        }
    }

    public override void Delete(bool OnlyRemovingComponent = true)
    {
        for(int i = 0; i < mesh.data.Submeshes.Count; i++)
        {
            GraphicsCache.RemoveVAO(mesh.path + "_" + i);
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
        
        if(Shaders[index] != null)
            Shaders[index].Delete();
        Shaders[index] = passed;
    }
    
    public void SetShaders(List<Shader> passed)
    {
        int least = Math.Min(Shaders.Count, passed.Count);

        for(int i = 0; i < least; i++)
        {
            if(Shaders[i] != null)
                Shaders[i].Delete();
            Shaders[i] = passed[i];
        }
    }

    public override void HandleFrozenStateChanged(bool frozen)
    {
        if(frozen)
        {
            (Vector3 AABBMin, Vector3 AABBMax) = mesh.data.GetWorldSpaceAABB(GameObject.Transform.ModelMatrix);
            ContainerNode  = GraphicsCache.Tree.RegisterComponentGetNode(this, AABBMin, AABBMax);
        }
    }

    public override void Render()
    {
        if(IsHidden || (this.GameObject.IsFrozen && ContainerNode.Culled))
            return;

        for(int i = 0; i < MeshVAOs.Count; i++)
        {
            Shaders[i].SetUniform("model", this.Transform.ModelMatrix);
            Shaders[i].SetUniform("lightIndices", new int[4] {0, 1, 2, 3});
            Shaders[i].Bind();
            
            GL.BindVertexArray(MeshVAOs[i]);
            GL.DrawElements(PrimitiveType.Triangles, mesh.data.Submeshes[i].Indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            Shaders[i].Unbind();

            GraphicsCache.DrawCallsThisFrame++;
            GraphicsCache.MeshDrawCallsThisFrame++;
        }
    }
}
