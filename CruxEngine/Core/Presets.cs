using CruxEngine.Components;
using CruxEngine.Graphics;
using CruxEngine.Graphics.Shaders;
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

    public static GameObject MakePhysicsObject(string model, string texture)
    {
        GameObject target = MakeObject(model, texture);
        target.AddComponent<MeshBoundsColliderComponent>();
        target.AddComponent<PhysicsComponent>();
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
            LoadPresetShader(ShaderPresets.Lit_3D, false, texture),
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
            Mats.Add(LoadPresetShader(ShaderPresets.Lit_3D, false, textures[i]));
        }
        target.AddComponent<MeshRenderComponent>()!.SetShaders(Mats);
        //target.AddComponent<MeshBoundsColliderComponent>();

        return target;
    }

    public static string MissingTexturePath = "CruxEngine/Assets/Textures/Required/Missing.jpg";

    public enum ShaderPresets
    {
        Lit_3D,
        Unlit_3D,
        Unlit_2D,
        Unlit_2D_Skybox
    }

    public static Shader LoadPresetShader(ShaderPresets shaderPreset, bool useInstancing, string texturePath = "")
    {
        return shaderPreset switch
        {
            ShaderPresets.Lit_3D => new Shader
            (
                "CruxEngine/Assets/Shaders/Required/Vertex/vert_3d.glsl",
                "CruxEngine/Assets/Shaders/Required/Fragment/frag_3d_lit.glsl",
                DataProvider.EmbeddedFileExists(texturePath) ? texturePath : MissingTexturePath,
                useInstancing
            ),
            ShaderPresets.Unlit_3D => new Shader
            (
                "CruxEngine/Assets/Shaders/Required/Vertex/vert_3d.glsl",
                "CruxEngine/Assets/Shaders/Required/Fragment/frag_3d_unlit.glsl",
                "",
                useInstancing
            ),
            ShaderPresets.Unlit_2D => new Shader
            (
                "CruxEngine/Assets/Shaders/Required/Vertex/vert_2d.glsl",
                "CruxEngine/Assets/Shaders/Required/Fragment/frag_2d_unlit.glsl",
                DataProvider.EmbeddedFileExists(texturePath) ? texturePath : MissingTexturePath,
                useInstancing
            ),
            ShaderPresets.Unlit_2D_Skybox => new SkyboxShader
            (
                "CruxEngine/Assets/Shaders/Required/Vertex/vert_2d.glsl",
                "CruxEngine/Assets/Shaders/Required/Fragment/frag_2d_unlit_skybox.glsl",
                "",
                useInstancing
            ),
            _ => null!,
        };
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
