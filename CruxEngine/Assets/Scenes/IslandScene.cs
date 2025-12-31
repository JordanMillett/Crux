using CruxEngine.Components;
using CruxEngine.Utilities.Helpers;
using CruxEngine.Utilities.IO;

namespace CruxEngine.Assets.Scenes;

public class IslandScene : Scene
{
    TransformComponent? CenterPoint;

    protected override void OnStart()
    {
        AssetHandler.GameAssetPath = "CruxEngine/Assets";

        //Skybox
        Lighting.AmbientColor = ColorHelper.HexToColor4("6f7290");
        float intensity = 1.25f;
        Lighting.SunColor = new Color4(intensity, intensity, intensity, 1f);

        Crux.Camera!.Transform.WorldPosition = new Vector3(0, 5f, 15f);
        Crux.Camera.Transform.LocalEulerAngles = new Vector3(-25f, 180f, 0f);

        CenterPoint = Crux.Engine.InstantiateGameObject().Transform;
        
        Crux.Camera.Transform.Parent = CenterPoint;

        //Dictionary<string, GameObject> Map = GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Island.gltf")!;
        GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Island.gltf");
        
        Crux.Engine.SetupDebugCanvas();
    }

    protected override void OnUpdate()
    {
        CenterPoint!.Transform.WorldRotation *= Quaternion.FromEulerAngles(0f, Crux.Engine.deltaTime * 0.5f, 0f);
    }
}
