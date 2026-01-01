global using OpenTK.Mathematics;
global using System.Text;
global using CruxEngine.Core;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using CruxEngine.Components;
using CruxEngine.Graphics;
using CruxEngine.Physics;
using CruxEngine.Utilities.IO;
using CruxEngine.Utilities;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace CruxEngine.Core;

public static class Crux
{
    public static GameEngine Engine { get; internal set; } = null!;
    internal static GameEngine Hidden { get; set; } = null!;
    public static CameraComponent Camera {get => Engine.Camera!; internal set => Engine.Camera = value;}
    public static CanvasComponent Canvas {get => Engine.Canvas!; internal set => Engine.Canvas = value;}
}

public class GameEngine : GameWindow
{
    public static readonly VersionData Version = new(0, 0, 2);
    public static int BuildNumber = 0;

    public event Action? EngineUpdateEvent;
    public event Action? EngineReadyEvent;

    public List<Vector3> DebugDisplayPositions = new List<Vector3>();
    public Vector2i Resolution { get; private set; } = new Vector2i(1280, 720);
    public Scene ActiveScene { get => activeScene!; private set => activeScene = value; }
    
    public float DpiMultiplier { get; private set; } = 1.0f;
    public float deltaTime { get; private set; } = 0f; 
    public float totalTime { get; private set; } = 0f;
    public float fixedTotalTime { get; private set; } = 0f;
    public float fixedDeltaTime { get; init; } = 1f / 60f;

    internal CameraComponent? Camera;
    internal CanvasComponent? Canvas;

    private Timer? physicsTimer;
    private Scene? activeScene = null;

    private int physicsFrameCalls = 0;
    private int frameCount = 0;
    private float frameTimer = 0f;
    
    public GameEngine(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        this.VSync = VSyncMode.On;
        this.ClientSize = Resolution;
        this.Title = GetWindowShortName();
        this.Icon = DataProvider.LoadIcon();

        Crux.Engine = this;
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);

        this.TryGetCurrentMonitorScale(out float horizontalDpi, out float verticalDpi);
        DpiMultiplier = Math.Max(horizontalDpi, verticalDpi);
        Resolution = new Vector2i(e.Width, e.Height);
        
        Camera?.Recalculate();
    }
    
    public GameObject InstantiateGameObject(string name = "")
    {
        if(ActiveScene == null)
        {
            Logger.LogWarning ("Failed to instantiate gameobject, there is no active scene.");
            return null!;
        }

        string fullName = String.IsNullOrEmpty(name) ? "GameObject #" + ActiveScene.Instantiated.Count : name;

        GameObject gameObject = new GameObject(fullName, ActiveScene);
        ActiveScene.Instantiated.Add(gameObject);

        if(Debug.FlagEnabled("LogCreated"))
            Logger.Log($"GameObject '{fullName}' created.");

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
            return $"DEBUG {GetEngineShortName()}";
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

    public static string GetSystemInformation()
    {
        return $"Sys {GetArchitecture()} {GetOperatingSystem()}";
    }

    public static string GetApplicationInformation()
    {
        return $"App {(Environment.Is64BitProcess ? "x64" : "x86")} {GetOperatingSystem()}";
    }

    public static string GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";

        return "Unknown";
    }

    static string GetArchitecture()
    {
        Architecture architecture = RuntimeInformation.OSArchitecture;
        return architecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => "unknown"
        };
    }

    protected override void OnLoad()
    {
        if(InDebugMode() && Debug.FlagEnabled("EnableProfiler"))
        {
            Profiler.Start();
            Input.CreateAction("Report Profiler", Keys.Backspace, true);
        }

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

        //Register engine keys
        Input.CreateAction("Unfocus Window", Keys.Escape, true);
        Input.CreateAction("Take Screenshot", Keys.F12, true);
        Input.CreateAction("Restart Scene", Keys.GraveAccent, true);

        //Required Objects INIT
        GameObject cam = new GameObject("Camera", null!);
        cam.AddComponent<CameraComponent>();
        
        //Scene Begin
        EngineReadyEvent?.Invoke();
        
        //Physics Begin
        physicsTimer = new Timer(OnPhysicsUpdate, null, 0, (int)(fixedDeltaTime * 1000));
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        
        Logger.Log("Game Client Stopped.", LogSource.System);
        Logger.Log("Engine Stopped.", LogSource.System);

        Logger.WritePendingLogsToFile();

        if(InDebugMode())
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Logger.LogPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }catch{}
        }
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
        
        if (InDebugMode() && Debug.FlagEnabled("EnableProfiler") && Input.IsActionPressed("report profiler"))
            Profiler.Report();
        if (Input.IsActionPressed("take screenshot"))
            TakeScreenshot();
        if (Input.IsActionPressed("unfocus window"))
            CursorState = CursorState.Normal;
        if (MouseState.IsButtonDown(MouseButton.Left))
            CursorState = CursorState.Grabbed;
        
        EngineUpdateEvent?.Invoke();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        //Reset
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GraphicsCache.DrawCallsLastFrame = GraphicsCache.DrawCallsThisFrame;
        GraphicsCache.TrianglesLastFrame = GraphicsCache.TrianglesThisFrame;
        GraphicsCache.LinesLastFrame = GraphicsCache.LinesThisFrame;

        GraphicsCache.DrawCallsThisFrame = 0;
        GraphicsCache.TrianglesThisFrame = 0;
        GraphicsCache.LinesThisFrame = 0;
        foreach (var key in InstancedMeshRenderComponent.Rendered.Keys.ToList())
            InstancedMeshRenderComponent.Rendered[key] = false;
        foreach (var key in GraphicsCache.VAOs.Keys.ToList())
            GraphicsCache.VAOs[key].meshBuffer.DrawnThisFrame = false;

        //Shadow Map Pass
        //ActiveScene.RenderSkyboxShadow

        //Main Render Pass
        ActiveScene!.RenderSkybox();

        try
        {
            foreach(GameObject E in ActiveScene.Instantiated)
            {
                if(E.HasComponent<RenderComponent>())
                    E.GetComponent<RenderComponent>()!.Render();
            }
        }catch
        {
            Logger.LogWarning("Failed to render frame, Instantiated objects was modified in runtime.");
        }

        frameTimer += (float) e.Time;
        frameCount++;
        if (frameTimer >= 0.25f)
        {
            GraphicsCache.FramesPerSecond = frameCount / frameTimer;
            frameCount = 0;
            frameTimer = 0f;
        }

        try
        {
            foreach(GameObject E in ActiveScene.Instantiated)
            {
                if(E.HasComponent<CanvasComponent>())
                    E.GetComponent<CanvasComponent>()!.AfterRender();
            }
        }catch
        {
            Logger.LogWarning("Failed to render after frame, Instantiated objects was modified in runtime.");
        }

        SwapBuffers();
    }

    /*
        if (Input.IsActionPressed("restart scene"))
        {
            ActiveScene = Crux.Engine.SetScene(new GameScene());
            return;
        }
        */

    public void SetCamera(CameraComponent camera)
    {
        Camera = camera;
    }

    public void SetCanvas(CanvasComponent canvas)
    {
        Canvas = canvas;
    }

    public void SetScene(Scene Selected) //add proper unloading and reloading instead of deleting?
    {
        if(Debug.FlagEnabled("MeasureSceneTransitionTime"))
            Logger.StartTimer("Scene Transition Time");

        if(ActiveScene != null)
            Logger.Log($"Deleting Scene '{ActiveScene.GetType().Name}'", LogSource.System);

        Input.UnbindAll();
        Camera?.GameObject.Delete(); //This prunes the camera of all other components
        for(int i = 0; i < ActiveScene?.Instantiated.Count; i++)
            ActiveScene.Instantiated[i].Delete();
        
        Logger.Log($"Loading Scene '{Selected.GetType().Name}'", LogSource.System);

        ActiveScene = Selected;
        ActiveScene.Start();

        Logger.Log($"Scene Set to '{ActiveScene.GetType().Name}'", LogSource.System);

        if(Debug.FlagEnabled("MeasureSceneTransitionTime"))
            Logger.EndTimer();

        if(Debug.FlagEnabled("OutputKeyBindings")) 
            Input.OutputKeyBindings();
    }

    void TakeScreenshot()
    {
        string picturesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Crux");
        if (!Directory.Exists(picturesPath))
            Directory.CreateDirectory(picturesPath);

        string filename = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss} {GetEngineShortName()}.png";
        string filePath = Path.Combine(picturesPath, filename);

        int width = Resolution.X;
        int height = Resolution.Y;

        // Read pixels from OpenGL buffer
        byte[] pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        // Flip the image vertically (OpenGL saves it upside-down)
        byte[] flippedPixels = new byte[pixels.Length];
        int rowSize = width * 4;
        for (int y = 0; y < height; y++)
        {
            Array.Copy(pixels, y * rowSize, flippedPixels, (height - 1 - y) * rowSize, rowSize);
        }
        
        var writer = new StbImageWriteSharp.ImageWriter();
        using (var fs = File.OpenWrite(filePath))
        {
            writer.WritePng(flippedPixels, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, fs);
        }
        
        Logger.Log($"Screenshot taken '{filePath}'", LogSource.System);
    }

    public void SetupDebugCanvas()
    {
        CanvasComponent Canvas = InstantiateGameObject("Canvas").AddComponent<CanvasComponent>()!;
        Canvas.ParseMarkup("CruxEngine/Assets/CUI/debug.html");
        Canvas.BindPoints.Add("FPS", () => GraphicsCache.FramesPerSecond.ToString("F2"));
        Canvas.BindPoints.Add("DrawCalls", () => GraphicsCache.DrawCallsLastFrame.ToString());
        Canvas.BindPoints.Add("Triangles", () => GraphicsCache.TrianglesLastFrame.ToString());
        Canvas.BindPoints.Add("Lines", () => GraphicsCache.LinesLastFrame.ToString());
        Canvas.BindPoints.Add("VAOs", () => GraphicsCache.VAOs.Count.ToString());
        Canvas.BindPoints.Add("Textures", () => GraphicsCache.Textures.Count.ToString());
        Canvas.BindPoints.Add("PhyFPS", () => PhysicsSystem.FramesPerSecond.ToString());
        Canvas.BindPoints.Add("Colliders", () => PhysicsSystem.TotalColliders.ToString());
        Canvas.BindPoints.Add("Objects", () => PhysicsSystem.TotalPhysicsObjects.ToString());
        Canvas.BindPoints.Add("Spheres", () => PhysicsSystem.SphereChecks.ToString());
        Canvas.BindPoints.Add("AABBs", () => PhysicsSystem.AABBChecks.ToString());
        Canvas.BindPoints.Add("OBBs", () => PhysicsSystem.OBBChecks.ToString());
        Canvas.BindPoints.Add("Sys", () => GameEngine.GetSystemInformation());
        Canvas.BindPoints.Add("App", () => GameEngine.GetApplicationInformation());
        Canvas.BindPoints.Add("Game", () => GameEngine.GetGameShortName());
        Canvas.BindPoints.Add("Engine", () => GameEngine.GetEngineShortName());
        
        SetCanvas(Canvas);
    }
}
