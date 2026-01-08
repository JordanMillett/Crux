using OpenTK.Windowing.GraphicsLibraryFramework;
using CruxEngine.Components;
using CruxEngine.Physics;
using CruxEngine.Graphics;
using CruxEngine.Utilities.IO;

namespace CruxEngine.Assets.Scenes;

public class PhysicsScene : Scene
{
    protected override string DefaultSkyboxPath => "CruxEngine/Assets/Presets/Skybox/Physics.json";
    protected override string DefaultSceneLightingPath => "CruxEngine/Assets/Presets/SceneLighting/Physics.json";

    protected override void OnStart()
    {
        Utilities.IO.DataProvider.RootDirectory = "CruxEngine/Assets";

        Dictionary<string, GameObject> Map = GltfHandler.LoadGltfAsMeshRenderers("CruxEngine/Assets/Models/Examples/Physics.gltf");
        foreach(string key in Map.Keys)
        {
            Map[key].AddComponent<MeshBoundsColliderComponent>();
            Map[key].GetComponent<ColliderComponent>()!.CalculateWorldBounds();
            Map[key].GetComponent<MeshRenderComponent>()!.GameObject.Freeze();
        }

        Crux.Camera?.GameObject.AddComponent<FreeLookComponent>();
        
        Crux.Camera!.Transform.WorldPosition = new Vector3(0, 1f, 0);

        Input.CreateAction("Spawn Cubes", Keys.Q);
        Input.CreateAction("Spawn Cube", Keys.E);
        Input.CreateAction("Time Physics", Keys.R);

        Input.CreateAction("Decrease Solver", Keys.LeftBracket);
        Input.CreateAction("Increase Solver", Keys.RightBracket);

        Input.CreateAction("Spawn Stress Test", Keys.T);

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

        if(Input.IsActionPressed("Time Physics"))
            PhysicsSystem.TimeNextPhysicsStep = true;
        

        if(Input.IsActionPressed("Spawn Cube") || Input.IsActionHeld("Spawn Cubes"))
        {
            CreateCube(Crux.Camera!.Transform.WorldPosition + (Crux.Camera.Transform.Forward * 3f));
        }

        if(Input.IsActionPressed("Spawn Stress Test"))
        {
            Vector3 Offset = new Vector3(-2f, 1f, 8f);

            for(int x = 0; x < 5; x++)
            {
                for(int y = 0; y < 5; y++)
                {
                    for(int z = 0; z < 5; z++)
                    {
                        CreateCube(new Vector3(x, y, z) + Offset);
                    }
                }
            }
        }
    }

    void CreateCube(Vector3 Position)
    {
        string debugTexture = "CruxEngine/Assets/Textures/Required/Debug.jpg";
        GameObject selected = Presets.MakePhysicsPrimitive(Primitives.Cube, debugTexture);
        selected.Transform.WorldPosition = Position;
        //selected.GetComponent<PhysicsComponent>().Mass = 1.0f;
    }
}
