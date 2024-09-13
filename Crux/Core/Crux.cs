global using OpenTK.Mathematics;
global using System.Text;
global using Crux.Core;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Crux.Components;
using Crux.Graphics;
using Crux.Physics;
using Crux.Utilities.IO;
using Crux.Utilities;

namespace Crux.Core;

public class GameEngine : GameWindow
{
    private static GameEngine? link;
    public static GameEngine Link
    {
        get
        {
            if (link == null)
                throw new InvalidOperationException("GameEngine is null");
            return link;
        }
    }
    
    public List<GameObject> Instantiated = new List<GameObject>();

    public Scene ActiveScene = null!;

    public event Action? OnUpdateCallback;
    public Action? OnEngineReadyCallback;

    public float deltaTime = 0f;
    public float totalTime = 0f;

    public float fixedTotalTime = 0f;

    public float fixedDeltaTime = 1f / 60f;
    Timer physicsTimer;
    float physicsFrameTimer = 0f;
    int physicsFrameCalls = 0;

    float frameTimer = 0f;
    int frameCount = 0;

    public Vector2i Resolution = new Vector2i(1280, 720);

    public CameraComponent Camera = null!;
    public TextRenderComponent DebugHUD = null!;

    public List<Vector3> DebugDisplayPositions = new List<Vector3>();

    public static readonly VersionData Version = new(0, 0, 1);

    public static int BuildNumber = 0;
    
    public GameEngine(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        link = this;
        this.VSync = VSyncMode.On;
        this.ClientSize = Resolution;
        this.Title = GetWindowShortName();
        this.Icon = AssetHandler.LoadIcon();
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
        
        Resolution = new Vector2i(e.Width, e.Height);
        
        if(Camera != null)
            Camera.Recalculate();
    }
    
    public GameObject CloneGameObject(GameObject toCopy)
    {
        GameObject gameObject = toCopy.Clone();
        Instantiated.Add(gameObject);

        return gameObject;
    }
    
    public GameObject InstantiateGameObject(string name = "")
    {
        string fullName = String.IsNullOrEmpty(name) ? "GameObject #" + Instantiated.Count : name;

        GameObject gameObject = new GameObject(fullName);
        Instantiated.Add(gameObject);

        return gameObject;
    }

    public static bool InDebugMode()
    {
        #if DEBUG
            return true;
        #else
            return false;
        #endif
    }

    public static string GetWindowShortName()
    {
        if(InDebugMode())
            return GetEngineShortName();
        else
            return GetGameShortName();   
    }

    public static string GetEngineShortName()
    {
        return $"Crux {Version} - Build {BuildNumber}";
    }

    public static string GetGameShortName()
    {
        string version = "0.0.1";

        return $"Game {version}";
    }

    protected override void OnLoad()
    {
        Logger.Log("Engine Loading...", LogSource.System);
        Logger.Log(GetEngineShortName(), LogSource.System);
        Logger.Log($"OpenGL {GL.GetString(StringName.Version)}", LogSource.System);
        Logger.Log($"Process ID {Environment.ProcessId}", LogSource.System);

        base.OnLoad();
        
        //OpenGL INIT
        GL.Viewport(0, 0, Size.X, Size.Y);
        GL.ClearColor(Color4.Black); // Set the clear color to black
        
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.DepthMask(true);
        
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        //Window INIT
        CursorState = CursorState.Grabbed;

        Logger.Log("Engine Started!", LogSource.System);

        //Required Objects INIT
        GameObject cam = InstantiateGameObject("Camera");
        cam.AddComponent<CameraComponent>();

        DebugHUD = InstantiateGameObject("HUD").AddComponent<TextRenderComponent>();
        DebugHUD.FontScale = 0.3f;
        DebugHUD.StartPosition = new Vector2(-1f, 0.95f);
        
        //Scene Begin
        OnEngineReadyCallback?.Invoke();
        
        //Physics Begin
        physicsTimer = new Timer(OnPhysicsUpdate, null, 0, (int)(fixedDeltaTime * 1000));
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        
        Logger.Log("Game Stopped.", LogSource.System);
        Logger.Log("Engine Stopped.", LogSource.System);
    }

    private void OnPhysicsUpdate(object? state)
    {
        physicsFrameCalls++;
        if (physicsFrameCalls >= 60)
        {
            PhysicsSystem.FramesPerSecond = PhysicsSystem.PhysicsFrameCount / 1f;
            PhysicsSystem.PhysicsFrameCount = 0;
            physicsFrameCalls = 0;
        }

        PhysicsSystem.Update();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        
        deltaTime = (float) e.Time;
        totalTime += deltaTime;
        
        if (IsKeyDown(Keys.Escape))
            CursorState = CursorState.Normal;
        if (MouseState.IsButtonDown(MouseButton.Left))
            CursorState = CursorState.Grabbed;
        
        OnUpdateCallback?.Invoke();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        //RESET
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GraphicsCache.DrawCallsThisFrame = 0;
        GraphicsCache.MeshDrawCallsThisFrame = 0;
        foreach (var key in InstancedMeshRenderComponent.Rendered.Keys.ToList())
            InstancedMeshRenderComponent.Rendered[key] = false;
        foreach (var key in TextRenderComponent.Rendered.Keys.ToList())
            TextRenderComponent.Rendered[key] = false;

        //RENDER
        ActiveScene.RenderSkybox();

        try
        {
            foreach(GameObject E in Instantiated)
            {
                E.GetComponents<RenderComponent>().ForEach(renderComponent => renderComponent.Render());
            }
        }catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Failed to render frame, Instantiated objects was modified in runtime.");
            //Console.WriteLine($"Iteration Error: {ex.Message}");
        }

        frameTimer += (float) e.Time;
        frameCount++;
        if (frameTimer >= 0.25f)
        {
            GraphicsCache.FramesPerSecond = frameCount / frameTimer;
            frameCount = 0;
            frameTimer = 0f;
        }

        DebugHUD.Text = GraphicsCache.GetShortInfo();
        DebugHUD.Text += AssetHandler.GetShortInfo();
        DebugHUD.Text += PhysicsSystem.GetShortInfo();

        //Console.WriteLine(GraphicsCache.GetFullInfo());
        /*
        foreach (var entry in InstancedMeshRenderComponent.InstanceData)
        {
            var pair = entry.Key; // (int vao, int vbo)
            var transforms = entry.Value.Transforms;
            var gpuBufferLength = entry.Value.GPUBufferLength;

            Console.WriteLine($"VAO: {pair.vao}, VBO: {pair.vbo}");
            Console.WriteLine($"Number of Transforms: {transforms.Count}");
            Console.WriteLine($"GPU Buffer Length: {gpuBufferLength}");
            Console.WriteLine("--------------------------------------------------");
        }*/

        SwapBuffers();
    }
}
