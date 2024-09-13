using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class MeshComponent : Component
{
    public string path { get; set; } = "";
    
    public Mesh data { get; set; } = null!;

    public MeshComponent(GameObject gameObject): base(gameObject)
    {               
        
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");
        sb.AppendLine($"- Vertices: {data.Vertices.Length}");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new MeshComponent(gameObject)
        {
            path = this.path,
            data = this.data.Clone()
        };
    }
    
    public void Load(string desired)
    {
        string extension = Path.GetExtension(desired).ToLower();
        
        switch (extension)
        {
            case ".obj":
                data = ObjHandler.LoadObjAsMesh(ref desired);
                break;
            case ".gltf":
                data = GltfHandler.LoadGltfAsMesh(ref desired, out Vector3 pos, out Quaternion rot, out Vector3 scale);
                Transform.WorldPosition = pos;
                Transform.WorldRotation = rot;
                Transform.Scale = scale;
                break;
        }
        
        path = desired;
    }
}
