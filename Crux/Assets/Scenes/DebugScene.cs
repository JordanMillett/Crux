namespace Crux.Assets.Scenes;

public class DebugScene : Scene
{
    /*
    GameObject Tester;

    TextRenderComponent Updater;
    TextRenderComponent Other;
    TextRenderComponent Other2;
    
    List<TransformComponent> DebugContacts = new List<TransformComponent>();

    int count = 0;
    ColliderComponent target = null;
    Vector3 offset = Vector3.Zero;
    float dist = 0f;
    */

    public override void Start()
    {
        /*
        GameEngine.Link.Camera.GameObject.AddComponent<FreeLookComponent>();

        GameEngine.Link.ActiveScene.Fog = Color4.Black;
        GameEngine.Link.ActiveScene.Skybox.SetUniform("topColor", Color4.Black);
        GameEngine.Link.ActiveScene.Skybox.SetUniform("bottomColor", Color4.Black);

        Presets.MakePrimitive(Primitives.Quad, "Crux/Assets/Textures/Debug.jpg").Transform.WorldRotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(90f), 0f, 0f);
        Presets.MakePrimitive(Primitives.Cube, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(2f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Cylinder, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(4f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Cone, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(6f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Sphere, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(8f, 0f, 0f);
        Presets.MakePrimitive(Primitives.Torus, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(10f, 0f, 0f);

        TransformComponent Bricks = Presets.MakePhysicsPrimitive(Primitives.Cube, "Crux/Assets/Textures/Debug.jpg").Transform;
        Bricks.WorldPosition = new Vector3(5f, 0f, 3f);
        
        GameObject Pole = Presets.MakePrimitive(Primitives.Cylinder, "Crux/Assets/Textures/Debug.jpg");
        Pole.Transform.WorldPosition = new Vector3(-3f, 2f, 2f);
        Pole.Transform.Scale = new Vector3(1f, 3f, 1f);
        Pole.AddComponent<MovementComponent>();

        Presets.MakeObject("Crux/Assets/Models/Test/Monkey.obj", "Crux/Assets/Textures/Missing.jpg").Transform.WorldPosition = new Vector3(5f, 3f, 0f);
        

        
        Presets.MakePhysicsPrimitive(Primitives.Cube, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(4f, 3f, 4f);
        Presets.MakePhysicsPrimitive(Primitives.Cube, "Crux/Assets/Textures/Debug.jpg").Transform.WorldPosition = new Vector3(4f, 3f, 3f);

        GameObject Spinner = Presets.MakePrimitive(Primitives.Torus, "Crux/Assets/Textures/Debug.jpg");
        Spinner.Transform.WorldPosition = new Vector3(3f, 2f, 2f);
        Spinner.AddComponent<MovementComponent>();


        
        GameObject Floor = Presets.MakePrimitive(Primitives.Cube, "Crux/Assets/Textures/Debug.jpg");
        Floor.Transform.WorldPosition = new Vector3(5f, -5f, 3f);
        Floor.Transform.Scale = new Vector3(5f, 1f, 5f);

        Updater = GameEngine.Link.InstantiateGameObject().AddComponent<TextRenderComponent>();
        Updater.FontScale = 0.3f;
        Updater.StartPosition = new Vector2(-1f, 0.95f);

        Other = GameEngine.Link.InstantiateGameObject().AddComponent<TextRenderComponent>();
        Other.FontScale = 0.3f;
        Other.StartPosition = new Vector2(-1f, 0.85f);

        Other2 = GameEngine.Link.InstantiateGameObject().AddComponent<TextRenderComponent>();
        Other2.FontScale = 0.3f;
        Other2.StartPosition = new Vector2(-1f, 0.75f);

        Tester = Presets.MakePhysicsPrimitive(Primitives.Cube, "Crux/Assets/Textures/Colors.jpg");
        Tester.Transform.WorldPosition = new Vector3(5f, -10f, 3f);

        GameEngine.Link.Camera.Transform.WorldPosition = new Vector3(5, 0.5f, 7);
        GameEngine.Link.Camera.GetComponent<FreeLookComponent>().yaw = MathHelper.DegreesToRadians(180f);
        */
    }

    public override void Update()
    {
        /*
        //Console.WriteLine(DebugContacts.Count);
        
        try{

        for(int i = 0; i < GameEngine.Link.DebugDisplayPositions.Count; i++)
        {
            if(i >= DebugContacts.Count)
            {
                TransformComponent next = GameEngine.Link.InstantiateGameObject().Transform;
                next.GameObject.AddComponent<LineRenderComponent>();
                next.WorldPosition = GameEngine.Link.DebugDisplayPositions[i];
                DebugContacts.Add(next);
            }else
            {
                DebugContacts[i].WorldPosition = GameEngine.Link.DebugDisplayPositions[i];
            }
        }}catch{}

        

        Other2.Text = $"{PhysicsSystem.TotalColliders} Colliders";
        Other.Text = GameEngine.Link.Camera.Transform.WorldPosition.ToString();

        if(target != null)
        {
            TransformComponent cam = GameEngine.Link.Camera.Transform;
            target.Transform.WorldPosition = (cam.WorldPosition + (cam.Forward * dist)) + offset;

            if (GameEngine.Link.IsKeyPressed(Keys.G))
            {
                if(!target.GameObject.HasComponent<PhysicsComponent>())
                    target.GameObject.AddComponent<PhysicsComponent>();

                target = null;
            }

            if(GameEngine.Link.MouseState.IsButtonPressed(MouseButton.Left))
            {
                target = null;
            }
        }else
        {
            Ray ray = new Ray(GameEngine.Link.Camera.Transform.WorldPosition, GameEngine.Link.Camera.Transform.Forward);
            RayHit hit;

            if(PhysicsSystem.Raycast(ray, out hit))
            {
                Updater.Text = hit.Collider.GameObject.name;

                if (GameEngine.Link.MouseState.IsButtonPressed(MouseButton.Left) && target == null)
                {
                    dist = hit.Distance;
                    offset = (hit.Collider.Transform.WorldPosition - hit.Point);
                    target = hit.Collider;
                }
                
            }else
            {
                Updater.Text = "";
            }
        }

        



        //Updater.Text = GameEngine.Link.Camera.Transform.WorldPosition.ToString();

        if (GameEngine.Link.IsKeyPressed(Keys.F) || GameEngine.Link.IsKeyDown(Keys.T))
        {
            for(int i = 0; i < 1; i++)
            {
                GameObject Spawned = Presets.MakePhysicsPrimitive(Primitives.Cube, "Crux/Assets/Textures/Missing.jpg");
                Spawned.Transform.WorldPosition = (GameEngine.Link.Camera.Transform.WorldPosition + (GameEngine.Link.Camera.Transform.Forward * 4f));
                Spawned.GetComponent<PhysicsComponent>().Velocity = Vector3.Zero; 
                Spawned.GetComponent<PhysicsComponent>().Awake = true; 
            }
        }
        

        if (GameEngine.Link.IsKeyDown(Keys.R))
        {
            Tester.Transform.WorldPosition = (GameEngine.Link.Camera.Transform.WorldPosition + (GameEngine.Link.Camera.Transform.Forward * 4f));
            Tester.GetComponent<PhysicsComponent>().Velocity = Vector3.Zero; 
            Tester.GetComponent<PhysicsComponent>().Awake = true; 
        }

        if (GameEngine.Link.IsKeyDown(Keys.Q))
        {
            Vector3 dir = Vector3.Normalize(GameEngine.Link.Camera.Transform.WorldPosition - Tester.Transform.WorldPosition);
            Tester.GetComponent<PhysicsComponent>().Velocity = dir * 10f;
            Tester.GetComponent<PhysicsComponent>().Awake = true; 
        }

        if (GameEngine.Link.IsKeyDown(Keys.E))
        {
            Vector3 dir = Vector3.Normalize(GameEngine.Link.Camera.Transform.WorldPosition - Tester.Transform.WorldPosition);
            Tester.GetComponent<PhysicsComponent>().Velocity = dir * -10f;
            Tester.GetComponent<PhysicsComponent>().Awake = true; 
        }

        /*
        if (count < 200)
        {
            Random R = new Random();

            GameObject Box = GameEngine.Link.InstantiateGameObject();
            Box.AddComponent<MeshComponent>().Load("Crux/Assets/Models/cube.obj");
            Box.AddComponent<MeshRenderComponent>().SetMaterial("Crux/Assets/Materials/box.json");
            Box.Transform.WorldPosition = new Vector3(
                    (float)(R.NextDouble() - 0.5) * 50,
                    (float)(R.NextDouble() - 0.5) * 50,
                    (float)((R.NextDouble() - 0.5) * 50) - 50
                );

            count++;
        }
        */
    }
}
