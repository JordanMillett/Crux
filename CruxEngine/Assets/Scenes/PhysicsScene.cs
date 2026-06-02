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
            Map[key].GetComponent<MeshRenderComponent>()!.GameObject.Freeze();
        }

        Crux.Camera?.GameObject.AddComponent<FreeLookComponent>();
        
        Crux.Camera!.Transform.WorldPosition = new Vector3(0, 1f, 0);

        Input.CreateAction("Spawn Cubes", Keys.Q);
        Input.CreateAction("Spawn Cube", Keys.E);
        Input.CreateAction("Time Physics", Keys.R);

        Input.CreateAction("Spawn Cube 2x", Keys.D2);
        Input.CreateAction("Spawn Cube 3x", Keys.D3);
        Input.CreateAction("Spawn Cube 4x", Keys.D4);
        Input.CreateAction("Spawn Cube 5x", Keys.D5);

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
        }

        if(Input.IsActionPressed("Increase Solver"))
        {
            PhysicsSystem.SolverIterations++;
        }

        if(Input.IsActionPressed("Time Physics"))
            PhysicsSystem.TimeNextPhysicsStep = true;
        

        if(Input.IsActionPressed("Spawn Cube") || Input.IsActionHeld("Spawn Cubes"))
        {
            CreateCube(Crux.Camera!.Transform.WorldPosition + (Crux.Camera.Transform.Forward * 3f));
        }

        if(Input.IsActionPressed("Spawn Cube 2x"))
            CreateCube(Crux.Camera!.Transform.WorldPosition + (Crux.Camera.Transform.Forward * 3f * 2f), 2f);
        if(Input.IsActionPressed("Spawn Cube 3x"))
            CreateCube(Crux.Camera!.Transform.WorldPosition + (Crux.Camera.Transform.Forward * 3f * 3f), 3f);
        if(Input.IsActionPressed("Spawn Cube 4x"))
            CreateCube(Crux.Camera!.Transform.WorldPosition + (Crux.Camera.Transform.Forward * 3f * 4f), 4f);
        if(Input.IsActionPressed("Spawn Cube 5x"))
            CreateCube(Crux.Camera!.Transform.WorldPosition + (Crux.Camera.Transform.Forward * 3f * 5f), 5f);

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

    void CreateCube(Vector3 Position, float size = 1f)
    {
        string debugTexture = "CruxEngine/Assets/Textures/Required/Debug.jpg";
        GameObject selected = Presets.MakePhysicsPrimitive(Primitives.Cube, debugTexture);
        selected.Transform.WorldPosition = Position;
        selected.Transform.Scale = new Vector3(size, size, size);
        selected.GetComponent<PhysicsComponent>()!.Mass = size * size;
    }
}
