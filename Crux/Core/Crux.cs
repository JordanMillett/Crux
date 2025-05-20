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
using System.Runtime.InteropServices;
using System.Diagnostics;

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
    Timer physicsTimer = null!;
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
        return $"Sys {GetArchitecture()} {GetOperatingSystem()}\n";
    }

    public static string GetApplicationInformation()
    {
        return $"App {(Environment.Is64BitProcess ? "x64" : "x86")} {GetOperatingSystem()}\n{GetGameShortName()}\n{GetEngineShortName()}\n";
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
        Input.CreateAction("Unfocus Window", Keys.Escape);
        Input.CreateAction("Take Screenshot", Keys.F12);

        //Required Objects INIT
        GameObject cam = InstantiateGameObject("Camera");
        cam.AddComponent<CameraComponent>();

        DebugHUD = InstantiateGameObject("HUD").AddComponent<TextRenderComponent>()!;
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
        
        if (Input.IsActionPressed("take screenshot"))
            TakeScreenshot();
        if (Input.IsActionPressed("unfocus window"))
            CursorState = CursorState.Normal;
        if (MouseState.IsButtonDown(MouseButton.Left))
            CursorState = CursorState.Grabbed;
        
        OnUpdateCallback?.Invoke();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        //Reset
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

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
        ActiveScene.RenderSkybox();

        try
        {
            foreach(GameObject E in Instantiated)
            {
                if(E.HasComponent<RenderComponent>())
                    E.GetComponent<RenderComponent>()!.Render();
                //E.GetComponents<RenderComponent>().ForEach(renderComponent => renderComponent.Render());
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

        DebugHUD.Text = GraphicsCache.GetShortInfo();
        DebugHUD.Text += AssetHandler.GetShortInfo();
        DebugHUD.Text += PhysicsSystem.GetShortInfo();
        DebugHUD.Text += GetSystemInformation();
        DebugHUD.Text += GetApplicationInformation();

        //Logger.Log(GraphicsCache.GetFullInfo());
        /*
        foreach (var entry in InstancedMeshRenderComponent.InstanceData)
        {
            var pair = entry.Key; // (int vao, int vbo)
            var transforms = entry.Value.Transforms;
            var gpuBufferLength = entry.Value.GPUBufferLength;

            Logger.Log($"VAO: {pair.vao}, VBO: {pair.vbo}");
            Logger.Log($"Number of Transforms: {transforms.Count}");
            Logger.Log($"GPU Buffer Length: {gpuBufferLength}");
            Logger.Log("--------------------------------------------------");
        }*/

        SwapBuffers();
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
}
