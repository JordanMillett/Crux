using OpenTK.Windowing.GraphicsLibraryFramework;
using CruxEngine.Components;
using CruxEngine.Physics;
using CruxEngine.Graphics;

namespace CruxEngine.Assets.Scenes;

public class DebugScene : Scene
{
    public override void Start()
    {
        //Skybox
        Fog = Color4.Black;
        Skybox.SetUniform("topColor", Color4.Black);
        Skybox.SetUniform("bottomColor", Color4.Black);

        Crux.Engine.Camera?.GameObject.AddComponent<FreeLookComponent>();
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
    
        Crux.Engine.Camera!.Transform.WorldPosition = new Vector3(5, 0.5f, 7);
        Crux.Engine.Camera.GetComponent<FreeLookComponent>().yaw = MathHelper.DegreesToRadians(180f);

        Input.CreateAction("Spawn Cube", Keys.Q);
        Input.CreateAction("Cast Ray", Keys.E);

        Crux.Canvas = Crux.Engine.SetupDebugCanvas();
    }

    public override void Update()
    {
        if(Input.IsActionHeld("Spawn Cube"))
        {
            string debugTexture = "CruxEngine/Assets/Textures/Required/Debug.jpg";

            GameObject selected = Presets.MakePhysicsPrimitive(Primitives.Cube, debugTexture);
            selected.Transform.WorldPosition = Crux.Engine.Camera!.Transform.WorldPosition + (Crux.Engine.Camera.Transform.Forward * 3f);
        }

        if(Input.IsActionHeld("Cast Ray"))
        {
            Ray ray = new Ray(Crux.Engine.Camera!.Transform.WorldPosition, Crux.Engine.Camera.Transform.Forward);
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
