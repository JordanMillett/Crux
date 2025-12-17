using CruxEngine.Components;
using CruxEngine.Utilities.Helpers;
using CruxEngine.Utilities.IO;

namespace CruxEngine.Assets.Scenes;

public class IslandScene : Scene
{
    TransformComponent? CenterPoint;

    public override void Start()
    {
        AssetHandler.GameAssetPath = "CruxEngine/Assets";

        //Skybox
        Ambient = ColorHelper.HexToColor4("6f7290");
        float intensity = 1.25f;
        Hue = new Color4(intensity, intensity, intensity, 1f);

        Crux.Engine.Camera!.Transform.WorldPosition = new Vector3(0, 5f, 15f);
        Crux.Engine.Camera.Transform.LocalEulerAngles = new Vector3(-25f, 180f, 0f);

        CenterPoint = Crux.Engine.InstantiateGameObject().Transform;
        
        Crux.Engine.Camera.Transform.Parent = CenterPoint;

        //Dictionary<string, GameObject> Map = GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Island.gltf")!;
        GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Island.gltf");
        
        Crux.Canvas = Crux.Engine.SetupDebugCanvas();
    }

    public override void Update()
    {
        CenterPoint!.Transform.WorldRotation *= Quaternion.FromEulerAngles(0f, Crux.Engine.deltaTime * 0.5f, 0f);
    }
}
