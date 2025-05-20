using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class InstancedMeshRenderComponent : RenderComponent
{
    public List<MeshBuffer> MeshBuffers { get; set; } = [];
    public static readonly Dictionary<MeshBuffer, bool> Rendered = [];
    public static readonly Dictionary<MeshBuffer, PerInstanceData> InstanceData = [];

    public struct PerInstanceData
    {
        public List<TransformComponent> Transforms;
        public Shader Mat;
        public int GPUBufferLength;
    }

    private readonly MeshComponent mesh;

    public Vector3 BoundsMin;
    public Vector3 BoundsMax;

    [Obsolete("Feature not maintained")]
    public InstancedMeshRenderComponent(GameObject gameObject): base(gameObject)
    {
        mesh = GetComponent<MeshComponent>();

        for(int i = 0; i < mesh.Data!.Submeshes.Count; i++)
        {
            float worldSize = MathF.Max(GraphicsCache.Tree.Root.Max.X, MathF.Max(GraphicsCache.Tree.Root.Max.Y, GraphicsCache.Tree.Root.Max.Z));
            float chunkSize = worldSize/2/2;
            int chunkX = (int)Math.Floor(Transform.WorldPosition.X / chunkSize);
            int chunkY = (int)Math.Floor(Transform.WorldPosition.Y / chunkSize);
            int chunkZ = (int)Math.Floor(Transform.WorldPosition.Z / chunkSize);

            BoundsMin = new Vector3(chunkX * chunkSize, chunkY * chunkSize, chunkZ * chunkSize);
            BoundsMax = new Vector3((chunkX + 1) * chunkSize, (chunkY + 1) * chunkSize, (chunkZ + 1) * chunkSize);

            string location = $"({chunkX},{chunkY},{chunkZ})";
            string nameKey = $"{mesh.LoadedPath}_{i}_{location}";

            MeshBuffer meshBuffer = GraphicsCache.GetInstancedMeshBuffer(nameKey, mesh.Data.Submeshes[i]);
            MeshBuffers.Add(meshBuffer);

            if(!InstanceData.ContainsKey(meshBuffer))
            {
                PerInstanceData data = new PerInstanceData
                {
                    Transforms = new List<TransformComponent> { this.Transform },
                    Mat = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Instance_Lit),
                    GPUBufferLength = 0
                };

                InstanceData.Add(meshBuffer, data);
                Rendered.Add(meshBuffer, false);
            }else
            {
                var entry = InstanceData[meshBuffer];
                entry.Transforms.Add(this.Transform);
                
                InstanceData[meshBuffer] = entry;
            }
        }
    }
    
    public override void Delete()
    {
        for(int i = 0; i < mesh.Data!.Submeshes.Count; i++)
        {
            PerInstanceData data = InstanceData[MeshBuffers[i]];
            data.Transforms.Remove(this.Transform);
            InstanceData[MeshBuffers[i]] = data;

            InstanceData[MeshBuffers[i]].Mat.Delete();

            float worldSize = MathF.Max(GraphicsCache.Tree.Root.Max.X, MathF.Max(GraphicsCache.Tree.Root.Max.Y, GraphicsCache.Tree.Root.Max.Z));
            float chunkSize = worldSize/2/2;
            int chunkX = (int)Math.Floor(Transform.WorldPosition.X / chunkSize);
            int chunkY = (int)Math.Floor(Transform.WorldPosition.Y / chunkSize);
            int chunkZ = (int)Math.Floor(Transform.WorldPosition.Z / chunkSize);

            string location = $"({chunkX},{chunkY},{chunkZ})";
            string nameKey = $"{mesh.LoadedPath}_{i}_{location}";

            GraphicsCache.RemoveBuffer(nameKey);
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
        InstancedMeshRenderComponent clone = new InstancedMeshRenderComponent(gameObject);
        return clone;
    }

    public override void HandleFrozenStateChanged(bool IsFrozen)
    {
        if(IsFrozen)
        {
            ContainerNode = GraphicsCache.Tree.RegisterComponentGetNode(this, BoundsMin, BoundsMax);
        }
    }
    
    public void SetMaterial(Shader mat, int index)
    {
        if(index >= MeshBuffers.Count)
            return;

        var pair = MeshBuffers[index];

        if (InstanceData.ContainsKey(pair))
        {
            var entry = InstanceData[pair];
            entry.Mat.Delete(); // Delete of the old material
            entry.Mat = mat; // Set new material
            InstanceData[pair] = entry;
        }
    }
    
    public void SetMaterials(List<Shader> mats)
    {
        int least = Math.Min(MeshBuffers.Count, mats.Count);

        for (int i = 0; i < least; i++)
        {
            var pair = MeshBuffers[i];

            if (InstanceData.ContainsKey(pair))
            {
                var entry = InstanceData[pair];
                entry.Mat.Delete(); // Delete of the old material
                entry.Mat = mats[i]; // Set new material
                InstanceData[pair] = entry;
            }
        }
    }

    public override void Render()
    {
        int Matrix4SizeInBytes = 16 * sizeof(float);

        for(int i = 0; i < MeshBuffers.Count; i++) //for each submesh in the mesh
        {
            if(InstanceData.ContainsKey(MeshBuffers[i]))
            {
                PerInstanceData data = InstanceData[MeshBuffers[i]];

                if(!Rendered[MeshBuffers[i]])
                {
                    if(data.Transforms.Count > data.GPUBufferLength)
                    {
                        data.GPUBufferLength = Math.Max(64, data.GPUBufferLength * 2);
                        InstanceData[MeshBuffers[i]] = data;

                        GL.BindBuffer(BufferTarget.ArrayBuffer, MeshBuffers[i].DynamicVBO);
                        GL.BufferData(BufferTarget.ArrayBuffer, data.GPUBufferLength * Matrix4SizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                    }

                    if(this.GameObject.IsFrozen && ContainerNode!.Culled)
                    {
                        Rendered[MeshBuffers[i]] = true;
                        return;
                    }

                    //Logger.Log("STILL VISIBLE");
    
                    Matrix4[] instanceMatrices = new Matrix4[data.Transforms.Count];
                    for (int j = 0; j < data.Transforms.Count; j++)
                    {
                        instanceMatrices[j] = data.Transforms[j].ModelMatrix;
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, MeshBuffers[i].DynamicVBO);
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Transforms.Count * Matrix4SizeInBytes, instanceMatrices);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                    

                    data.Mat.Bind();

                    GL.BindVertexArray(MeshBuffers[i].DynamicVBO);
                    GL.DrawElementsInstanced(PrimitiveType.Triangles, mesh.Data!.Submeshes[i].Indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero, data.Transforms.Count);
                    GraphicsCache.DrawCallsThisFrame++;
                    
                    GL.BindVertexArray(0);
                    data.Mat.Unbind();
                    Rendered[MeshBuffers[i]] = true;
                }
            }
        }
    }
}
