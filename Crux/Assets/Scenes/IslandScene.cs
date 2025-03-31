using Crux.Components;
using Crux.Utilities.Helpers;
using Crux.Utilities.IO;

namespace Crux.Assets.Scenes;

public class IslandScene : Scene
{
    TransformComponent CenterPoint;

    public override void Start()
    {
        AssetHandler.GameAssetPath = "Crux/Assets";

        //Skybox
        Ambient = ColorHelper.HexToColor4("6f7290");
        float intensity = 1.25f;
        Hue = new Color4(intensity, intensity, intensity, 1f);

        GameEngine.Link.Camera.Transform.WorldPosition = new Vector3(0, 5f, 15f);
        GameEngine.Link.Camera.Transform.LocalEulerAngles = new Vector3(-25f, 180f, 0f);

        CenterPoint = GameEngine.Link.InstantiateGameObject().Transform;
        
        GameEngine.Link.Camera.Transform.Parent = CenterPoint;

        Dictionary<string, GameObject> Map = GltfHandler.LoadGltfAsMeshRenderers("Crux/Assets/Models/Examples/Island.gltf");

        
    }

    public override void Update()
    {
        CenterPoint.Transform.WorldRotation *= Quaternion.FromEulerAngles(0f, GameEngine.Link.deltaTime * 0.5f, 0f);
    }
}
