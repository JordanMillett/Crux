using OpenTK.Windowing.Desktop;
using Crux.Utilities.IO;

namespace Game;

class GameLauncher
{
    static void Main(string[] args)
    {
        NativeWindowSettings nativeWindowSettings = new NativeWindowSettings()
        {
            APIVersion = new System.Version(4, 3), //OpenGL 4.3
        };

        GameEngine.BuildNumber = AssetHandler.IterateBuildNumber();
        
        using (var engine = new GameEngine(GameWindowSettings.Default, nativeWindowSettings))
        {
            engine.Title = GameEngine.GetWindowShortName();
            engine.Icon = Crux.Utilities.IO.AssetHandler.LoadIcon();
            engine.ClientSize = new OpenTK.Mathematics.Vector2i(1280, 720);
            
            engine.OnEngineReadyCallback = () =>
            {
                GameInstance game = new GameInstance();
                game.Ready();
            };
            
            engine.Run();
        }
    }
}
