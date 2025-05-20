using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class MeshComponent : Component
{
    public string LoadedPath { get; set; } = "";
    
    public Mesh? Data { get; set; }

    public MeshComponent(GameObject gameObject): base(gameObject)
    {               
        
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");
        sb.AppendLine($"- Vertices: {Data!.Vertices.Length}");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new MeshComponent(gameObject)
        {
            LoadedPath = this.LoadedPath,
            Data = this.Data!.Clone()
        };
    }
    
    public void Load(string desired)
    {
        string extension = Path.GetExtension(desired).ToLower();
        
        switch (extension)
        {
            case ".obj":
                Data = ObjHandler.LoadObjAsMesh(ref desired);
                break;
            case ".gltf":
                Data = GltfHandler.LoadGltfAsMesh(ref desired, out Vector3 pos, out Quaternion rot, out Vector3 scale)!;
                Transform.WorldPosition = pos;
                Transform.WorldRotation = rot;
                Transform.Scale = scale;
                break;
        }
        
        LoadedPath = desired;
    }
}
