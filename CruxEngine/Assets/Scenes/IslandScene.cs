using CruxEngine.Components;
using CruxEngine.Graphics;
using CruxEngine.Utilities.Helpers;
using CruxEngine.Utilities.IO;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace CruxEngine.Assets.Scenes;

public class IslandScene : Scene
{
    TransformComponent? CenterPoint;

    protected override string DefaultSkyboxPath => "CruxEngine/Assets/Presets/Skybox/Island.json";
    protected override string DefaultSceneLightingPath => "CruxEngine/Assets/Presets/SceneLighting/Island.json";

    protected override void OnStart()
    {
        Crux.Camera!.Transform.WorldPosition = new Vector3(0, 5f, 15f);
        Crux.Camera.Transform.LocalEulerAngles = new Vector3(-25f, 180f, 0f);

        CenterPoint = Crux.Engine.InstantiateGameObject().Transform;
        
        Crux.Camera.Transform.Parent = CenterPoint;

        //Dictionary<string, GameObject> Map = GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Island.gltf")!;
        GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Island.gltf");
        
        Crux.Engine.SetupDebugCanvas();

        Input.CreateAction("Higher", Keys.RightBracket);
        Input.CreateAction("Lower", Keys.LeftBracket);
    }

    protected override void OnUpdate()
    {
        CenterPoint!.Transform.WorldRotation *= Quaternion.FromEulerAngles(0f, Crux.Engine.deltaTime * 0.5f, 0f);

        if(Input.IsActionPressed("Higher"))
        {
            Lighting.SunIntensity += 0.25f;
            Logger.Log(Lighting.SunIntensity);
        }
        if(Input.IsActionPressed("Lower"))
        {
            Lighting.SunIntensity -= 0.25f;
            Logger.Log(Lighting.SunIntensity);
        }
    }
}
