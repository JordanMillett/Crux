using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class InstancedMeshRenderComponent : RenderComponent
{
    public List<(int vao, int vbo)> MeshVAOs { get; set; } = [];
    public static Dictionary<(int vao, int vbo), bool> Rendered = [];
    public static Dictionary<(int vao, int vbo), PerInstanceData> InstanceData = [];

    public struct PerInstanceData
    {
        public List<TransformComponent> Transforms;
        public Shader Mat;
        public int GPUBufferLength;
    }

    MeshComponent mesh;

    public Vector3 BoundsMin;
    public Vector3 BoundsMax;

    public InstancedMeshRenderComponent(GameObject gameObject): base(gameObject)
    {
        mesh = GetComponent<MeshComponent>();

        for(int i = 0; i < mesh.data.Submeshes.Count; i++)
        {
            float worldSize = MathF.Max(GraphicsCache.Tree.Root.Max.X, MathF.Max(GraphicsCache.Tree.Root.Max.Y, GraphicsCache.Tree.Root.Max.Z));
            float chunkSize = worldSize/2/2;
            int chunkX = (int)Math.Floor(Transform.WorldPosition.X / chunkSize);
            int chunkY = (int)Math.Floor(Transform.WorldPosition.Y / chunkSize);
            int chunkZ = (int)Math.Floor(Transform.WorldPosition.Z / chunkSize);

            BoundsMin = new Vector3(chunkX * chunkSize, chunkY * chunkSize, chunkZ * chunkSize);
            BoundsMax = new Vector3((chunkX + 1) * chunkSize, (chunkY + 1) * chunkSize, (chunkZ + 1) * chunkSize);

            string location = $"({chunkX},{chunkY},{chunkZ})";
            string nameKey = $"{mesh.path}_{i}_{location}";
            
            var pair = GraphicsCache.GetInstancedMeshVAO(nameKey, mesh.data.Submeshes[i]);
            MeshVAOs.Add(pair);

            if(!InstanceData.ContainsKey(pair))
            {
                PerInstanceData data = new PerInstanceData
                {
                    Transforms = new List<TransformComponent> { this.Transform },
                    Mat = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Instance_Lit),
                    GPUBufferLength = 0
                };

                InstanceData.Add(pair, data);
                Rendered.Add(pair, false);
            }else
            {
                var entry = InstanceData[pair];
                entry.Transforms.Add(this.Transform);
                
                InstanceData[pair] = entry;
            }
        }
    }
    
    public override void Delete(bool OnlyRemovingComponent = true)
    {
        for(int i = 0; i < mesh.data.Submeshes.Count; i++)
        {
            PerInstanceData data = InstanceData[MeshVAOs[i]];
            data.Transforms.Remove(this.Transform);
            InstanceData[MeshVAOs[i]] = data;

            InstanceData[MeshVAOs[i]].Mat.Delete();

            float worldSize = MathF.Max(GraphicsCache.Tree.Root.Max.X, MathF.Max(GraphicsCache.Tree.Root.Max.Y, GraphicsCache.Tree.Root.Max.Z));
            float chunkSize = worldSize/2/2;
            int chunkX = (int)Math.Floor(Transform.WorldPosition.X / chunkSize);
            int chunkY = (int)Math.Floor(Transform.WorldPosition.Y / chunkSize);
            int chunkZ = (int)Math.Floor(Transform.WorldPosition.Z / chunkSize);

            string location = $"({chunkX},{chunkY},{chunkZ})";
            string nameKey = $"{mesh.path}_{i}_{location}";

            GraphicsCache.RemoveInstancedVAO(nameKey);
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

    public override void HandleFrozenStateChanged(bool frozen)
    {
        if(frozen)
        {
            ContainerNode = GraphicsCache.Tree.RegisterComponentGetNode(this, BoundsMin, BoundsMax);
        }
    }
    
    public void SetMaterial(Shader mat, int index)
    {
        if(index >= MeshVAOs.Count)
            return;

        var pair = MeshVAOs[index];

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
        int least = Math.Min(MeshVAOs.Count, mats.Count);

        for (int i = 0; i < least; i++)
        {
            var pair = MeshVAOs[i];

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

        for(int i = 0; i < MeshVAOs.Count; i++) //for each submesh in the mesh
        {
            if(InstanceData.ContainsKey(MeshVAOs[i]))
            {
                PerInstanceData data = InstanceData[MeshVAOs[i]];

                if(!Rendered[MeshVAOs[i]])
                {
                    /*
                    Random Rand = new Random();
                    if(Rand.NextDouble() > 0.75)
                    {
                        InstanceData[MeshVAOs[i]] = (transforms, mat, gpuBufferLength, true);
                        return;
                    }
                    Logger.Log(gpuBufferLength);
                    */

                    if(data.Transforms.Count > data.GPUBufferLength)
                    {
                        data.GPUBufferLength = Math.Max(64, data.GPUBufferLength * 2);
                        InstanceData[MeshVAOs[i]] = data;

                        GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVAOs[i].vbo);
                        GL.BufferData(BufferTarget.ArrayBuffer, data.GPUBufferLength * Matrix4SizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                    }

                    if(this.GameObject.IsFrozen && ContainerNode.Culled)
                    {
                        Rendered[MeshVAOs[i]] = true;
                        return;
                    }

                    //Logger.Log("STILL VISIBLE");
    
                    Matrix4[] instanceMatrices = new Matrix4[data.Transforms.Count];
                    for (int j = 0; j < data.Transforms.Count; j++)
                    {
                        instanceMatrices[j] = data.Transforms[j].ModelMatrix;
                    }

                    GL.BindBuffer(BufferTarget.ArrayBuffer, MeshVAOs[i].vbo);
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Transforms.Count * Matrix4SizeInBytes, instanceMatrices);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                    

                    data.Mat.Bind();

                    GL.BindVertexArray(MeshVAOs[i].vao);
                    GL.DrawElementsInstanced(PrimitiveType.Triangles, mesh.data.Submeshes[i].Indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero, data.Transforms.Count);
                    GraphicsCache.DrawCallsThisFrame++;
                    GraphicsCache.MeshDrawCallsThisFrame++;
                    
                    GL.BindVertexArray(0);
                    data.Mat.Unbind();
                    Rendered[MeshVAOs[i]] = true;
                }
            }
        }
    }
}
