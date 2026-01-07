using OpenTK.Windowing.GraphicsLibraryFramework;
using CruxEngine.Components;
using CruxEngine.Physics;
using CruxEngine.Graphics;

namespace CruxEngine.Assets.Scenes;

public class DebugScene : Scene
{
    protected override string DefaultSkyboxPath => "CruxEngine/Assets/Presets/Skybox/Debug.json";
    protected override string DefaultSceneLightingPath => "CruxEngine/Assets/Presets/SceneLighting/Debug.json";

    protected override void OnStart()
    {
        Utilities.IO.DataProvider.RootDirectory = "CruxEngine/Assets";
        //Skybox
        Lighting.FogColor = Color4.Black;

        Crux.Camera?.GameObject.AddComponent<FreeLookComponent>();
        string debugTexture = "CruxEngine/Assets/Textures/Required/Debug.jpg";

        GameObject selected;
        selected = Presets.MakePrimitive(Primitives.Quad, debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldRotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(90f), 0f, 0f);
        selected.Freeze();

        selected = Presets.MakePrimitive(Primitives.Cube, debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(2f, 0f, 0f);
        selected.Freeze();
        
        selected = Presets.MakePrimitive(Primitives.Cylinder, debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(4f, 0f, 0f);
        selected.Freeze();

        selected = Presets.MakePrimitive(Primitives.Cone, debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(6f, 0f, 0f);
        selected.Freeze();

        selected = Presets.MakePrimitive(Primitives.Sphere, debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(8f, 0f, 0f);
        selected.Freeze();

        selected = Presets.MakePrimitive(Primitives.Torus, debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(10f, 0f, 0f);
        selected.Freeze();

        selected = Presets.MakeObject("CruxEngine/Assets/Models/Required/Monkey.obj", debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(5f, 3f, 0f);
        selected.Freeze();

        selected = Crux.Engine.InstantiateGameObject();
        selected.Transform.WorldPosition = new Vector3(2f, 3f, 4f);
        
        selected = Presets.MakePrimitive(Primitives.Cube, debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(5f, -5f, 3f);
        selected.Transform.Scale = new Vector3(5f, 1f, 5f);
        selected.Freeze();
  
        selected = Presets.MakePrimitive(Primitives.Cylinder, debugTexture);
        selected.Transform.WorldPosition = new Vector3(-3f, 2f, 2f);
        selected.Transform.Scale = new Vector3(1f, 3f, 1f);
        selected.AddComponent<MovementComponent>();
        selected.AddComponent<MeshBoundsColliderComponent>();
        
        selected = Presets.MakePrimitive(Primitives.Torus, debugTexture);
        selected.Transform.WorldPosition = new Vector3(3f, 2f, 2f);
        selected.AddComponent<MovementComponent>();
        selected.AddComponent<MeshBoundsColliderComponent>();
    
        Crux.Camera!.Transform.WorldPosition = new Vector3(5, 0.5f, 7);
        Crux.Camera.GetComponent<FreeLookComponent>().yaw = MathHelper.DegreesToRadians(180f);

        Input.CreateAction("Spawn Cube", Keys.Q);
        Input.CreateAction("Cast Ray", Keys.E);

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

        if(Input.IsActionHeld("Cast Ray"))
        {
            Ray ray = new Ray(Crux.Camera!.Transform.WorldPosition, Crux.Camera.Transform.Forward);
            if(PhysicsSystem.Raycast(ray, out RayHit hit))
            {
                Logger.LogWarning(hit.Collider.GameObject.Name);

                GameObject LineObject = Crux.Engine.InstantiateGameObject();
                LineObject.Transform.WorldPosition = hit.Point;
                LineObject.AddComponent<LineRenderComponent>();
            }
        }
    }
}
