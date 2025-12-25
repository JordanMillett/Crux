global using OpenTK.Mathematics;
global using System.Text;
global using CruxEngine;
global using CruxEngine.Core;

using OpenTK.Windowing.Desktop;
using CruxEngine.Utilities.IO;

namespace Game;

class GameLauncher
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
            Debug.LoadFlags(args[0]);

        NativeWindowSettings nativeWindowSettings = new NativeWindowSettings()
        {
            APIVersion = new System.Version(4, 3), //OpenGL 4.3
        };

        GameEngine.BuildNumber = AssetHandler.IterateBuildNumber();

        using (var engine = new GameEngine(GameWindowSettings.Default, nativeWindowSettings))
        {
            engine.Title = GameEngine.GetWindowShortName();
            engine.Icon = AssetHandler.LoadIcon();
            engine.ClientSize = new OpenTK.Mathematics.Vector2i(1280, 720);
            
            engine.EngineReadyEvent = () =>
            {
                MyGame client = new MyGame();
                client.Start();
            };
            
            engine.Run();
        }
    }
}