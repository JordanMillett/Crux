using CruxEngine.Components;
using CruxEngine.Graphics;
using CruxEngine.Utilities.IO;

namespace CruxEngine.Core;

public enum Primitives
{
    Cube,
    Cone,
    Cylinder,
    Quad,
    Sphere,
    Torus
}

public static class Presets
{
    public static GameObject MakePrimitive(Primitives model, string texture)
    {
        return MakeObject("CruxEngine/Assets/Models/Primitives/" + model.ToString() + ".obj", texture);
    }
    
    public static GameObject MakePhysicsPrimitive(Primitives model, string texture)
    {
        GameObject target = MakePrimitive(model, texture);
        target.AddComponent<MeshBoundsColliderComponent>();
        target.AddComponent<PhysicsComponent>();
        return target;
    }

    public static GameObject MakeColliderObject(string model, string texture)
    {
        GameObject target = MakeObject(model, texture);
        target.AddComponent<MeshBoundsColliderComponent>();
        return target;
    }

    public static GameObject MakeObject(string model, string texture)
    {
        string textureName = Path.GetFileNameWithoutExtension(texture);
        string modelName = Path.GetFileNameWithoutExtension(model);

        GameObject target = Crux.Engine.InstantiateGameObject(textureName + " " + modelName);
        target.AddComponent<MeshComponent>()!.Load(model);
        target.AddComponent<MeshRenderComponent>()!.SetShader
        (
            AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Lit_3D, false, texture),
            0
        );

        return target;
    }
    
    public static GameObject MakeColliderObject(string model, List<string> textures)
    {
        GameObject target = MakeObject(model, textures);
        target.AddComponent<MeshBoundsColliderComponent>();
        return target;
    }

    public static GameObject MakeObject(string model, List<string> textures)
    {
        string modelName = Path.GetFileNameWithoutExtension(model);
        GameObject target = Crux.Engine.InstantiateGameObject(Path.GetFileNameWithoutExtension(textures[0]) + " " + modelName);
        target.AddComponent<MeshComponent>()!.Load(model);

        List<Shader> Mats = new List<Shader>();
        for(int i = 0; i < textures.Count; i++)
        {           
            Mats.Add(AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Lit_3D, false, textures[i]));
        }
        target.AddComponent<MeshRenderComponent>()!.SetShaders(Mats);
        //target.AddComponent<MeshBoundsColliderComponent>();

        return target;
    }
        
    /*
    GameObject MakeLight(Vector3 Pos)
    {
        GameObject Light = Crux.Engine.InstantiateGameObject();
        //Light.AddComponent<LineRenderComponent>();
        Light.AddComponent<LightComponent>();
        Light.Transform.WorldPosition = Pos;

        return Light;
    }*/
}
