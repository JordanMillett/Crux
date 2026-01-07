using OpenTK.Windowing.GraphicsLibraryFramework;
using CruxEngine.Components;
using CruxEngine.Physics;
using CruxEngine.Graphics;
using CruxEngine.Utilities.IO;

namespace CruxEngine.Assets.Scenes;

public class PhysicsScene : Scene
{
    protected override string DefaultSkyboxPath => "CruxEngine/Assets/Presets/Skybox/Debug.json";
    protected override string DefaultSceneLightingPath => "CruxEngine/Assets/Presets/SceneLighting/Debug.json";

    protected override void OnStart()
    {
        Utilities.IO.DataProvider.RootDirectory = "CruxEngine/Assets";

        Dictionary<string, GameObject> Map = GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Physics.gltf");
        foreach(string key in Map.Keys)
        {
            Map[key].AddComponent<MeshBoundsColliderComponent>()!.ColliderIndex = 1;
            Map[key].GetComponent<ColliderComponent>()!.ComputeBounds();
            Map[key].GetComponent<MeshRenderComponent>()!.GameObject.Freeze();
        }

        Crux.Camera?.GameObject.AddComponent<FreeLookComponent>();
        
        Crux.Camera!.Transform.WorldPosition = new Vector3(0, 1f, -1);
        Crux.Camera.GetComponent<FreeLookComponent>().yaw = MathHelper.DegreesToRadians(180f);

        Input.CreateAction("Spawn Cube", Keys.Q);

        Input.CreateAction("Decrease Solver", Keys.LeftBracket);
        Input.CreateAction("Increase Solver", Keys.RightBracket);

        Crux.Engine.SetupDebugCanvas();
    }

    protected override void OnUpdate()
    {
        
        if(Input.IsActionPressed("Decrease Solver"))
        {
            PhysicsSystem.SolverIterations = Math.Max(PhysicsSystem.SolverIterations - 1, 1);
            Logger.Log($"Solver Iterations: {PhysicsSystem.SolverIterations}");
        }
        if(Input.IsActionPressed("Increase Solver"))
        {
            PhysicsSystem.SolverIterations++;
            Logger.Log($"Solver Iterations: {PhysicsSystem.SolverIterations}");
        }
        

        if(Input.IsActionPressed("Spawn Cube"))
        {
            string debugTexture = "CruxEngine/Assets/Textures/Required/Debug.jpg";

            GameObject selected = Presets.MakePhysicsPrimitive(Primitives.Cube, debugTexture);
            selected.Transform.WorldPosition = Crux.Camera!.Transform.WorldPosition + (Crux.Camera.Transform.Forward * 3f);
            selected.GetComponent<PhysicsComponent>().Mass = 1.0f;
        }
    }
}
