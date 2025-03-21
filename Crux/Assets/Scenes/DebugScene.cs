using Crux.Components;

namespace Crux.Assets.Scenes;

public class DebugScene : Scene
{
    public override void Start()
    {
        //Skybox
        Fog = Color4.Black;
        Skybox.SetUniform("topColor", Color4.Black);
        Skybox.SetUniform("bottomColor", Color4.Black);

        GameEngine.Link.Camera.GameObject.AddComponent<FreeLookComponent>();

        Presets.MakePrimitive(Primitives.Quad, "Crux/Assets/Textures/Debug.jpg").Transform.WorldRotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(90f), 0f, 0f);
        Presets.MakePrimitive(Primitives.Cube, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(2f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Cylinder, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(4f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Cone, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(6f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Sphere, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(8f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Torus, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(10f, 0f, 0f);
  
        GameObject Pole = Presets.MakePrimitive(Primitives.Cylinder, "Crux/Assets/Textures/Debug.jpg");
        Pole.Transform.WorldPosition = new Vector3(-3f, 2f, 2f);
        Pole.Transform.Scale = new Vector3(1f, 3f, 1f);
        Pole.AddComponent<MovementComponent>();
        Pole.AddComponent<ColliderComponent>().ShowColliders = true;

        Presets.MakeObject("Crux/Assets/Models/Required/Monkey.obj", "Crux/Assets/Textures/Missing.jpg").Transform.WorldPosition = new Vector3(5f, 3f, 0f);
        
        GameObject Spinner = Presets.MakePrimitive(Primitives.Torus, "Crux/Assets/Textures/Debug.jpg");
        Spinner.Transform.WorldPosition = new Vector3(3f, 2f, 2f);
        Spinner.AddComponent<MovementComponent>();
        Spinner.AddComponent<ColliderComponent>().ShowColliders = true;

        GameObject Floor = Presets.MakePrimitive(Primitives.Cube, "Crux/Assets/Textures/Debug.jpg");
        Floor.Transform.WorldPosition = new Vector3(5f, -5f, 3f);
        Floor.Transform.Scale = new Vector3(5f, 1f, 5f);
    
        GameEngine.Link.Camera.Transform.WorldPosition = new Vector3(5, 0.5f, 7);
        GameEngine.Link.Camera.GetComponent<FreeLookComponent>().yaw = MathHelper.DegreesToRadians(180f);
    }

    public override void Update()
    {
        
    }
}
