using OpenTK.Windowing.GraphicsLibraryFramework;
using Crux.Components;
using Crux.Physics;
using Crux.Graphics;

namespace Crux.Assets.Scenes;

public class DebugScene : Scene
{
    public CanvasComponent? Canvas;

    public override void Start()
    {
        //Skybox
        Fog = Color4.Black;
        Skybox.SetUniform("topColor", Color4.Black);
        Skybox.SetUniform("bottomColor", Color4.Black);

        GameEngine.Link.Camera?.GameObject.AddComponent<FreeLookComponent>();
        string debugTexture = "Crux/Assets/Textures/Required/Debug.jpg";

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

        selected = Presets.MakeObject("Crux/Assets/Models/Required/Monkey.obj", debugTexture);
        selected.AddComponent<MeshBoundsColliderComponent>();
        selected.Transform.WorldPosition = new Vector3(5f, 3f, 0f);
        selected.Freeze();

        selected = GameEngine.Link.InstantiateGameObject();
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
    
        GameEngine.Link.Camera!.Transform.WorldPosition = new Vector3(5, 0.5f, 7);
        GameEngine.Link.Camera.GetComponent<FreeLookComponent>().yaw = MathHelper.DegreesToRadians(180f);

        Input.CreateAction("Spawn Cube", Keys.Q);
        Input.CreateAction("Cast Ray", Keys.E);
        Input.OutputKeyBindings();

        Canvas = GameEngine.Link.SetupDebugCanvas();
    }

    public override void Update()
    {
        if(Input.IsActionHeld("Spawn Cube"))
        {
            string debugTexture = "Crux/Assets/Textures/Required/Debug.jpg";

            GameObject selected = Presets.MakePhysicsPrimitive(Primitives.Cube, debugTexture);
            selected.Transform.WorldPosition = GameEngine.Link.Camera!.Transform.WorldPosition + (GameEngine.Link.Camera.Transform.Forward * 3f);
        }

        if(Input.IsActionHeld("Cast Ray"))
        {
            Ray ray = new Ray(GameEngine.Link.Camera!.Transform.WorldPosition, GameEngine.Link.Camera.Transform.Forward);
            if(PhysicsSystem.Raycast(ray, out RayHit hit))
            {
                Logger.LogWarning(hit.Collider.GameObject.Name);

                GameObject LineObject = GameEngine.Link.InstantiateGameObject();
                LineObject.Transform.WorldPosition = hit.Point;
                LineObject.AddComponent<LineRenderComponent>();
            }
        }
    }
}
