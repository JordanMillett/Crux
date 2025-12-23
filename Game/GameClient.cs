using CruxEngine.Assets.Scenes;
using Game.Assets.Scenes;

namespace Game;

public static class Game
{
    public static GameClient Client { get; internal set; } = null!;
    public static Scene ActiveScene => Client.ActiveScene!;
}

public class GameClient
{  
    public Scene ActiveScene { get; internal set; } = null!;

    public int Score = 0;
    
    public GameClient()
    {
        Game.Client = this;
    }

    public void Start()
    {
        Logger.Log("Game Client Loading...", LogSource.System);
        Crux.Engine.OnEngineUpdateCallback += Update;
        
        //ActiveScene = Crux.Engine.SetScene(new IslandScene());   
        //ActiveScene = Crux.Engine.SetScene(new DebugScene());  
        ActiveScene = Crux.Engine.SetScene(new GameScene()); 

        Logger.Log("Game Client Started!", LogSource.System);
    }
            
    public void Update()
    {
        if (Input.IsActionPressed("restart scene"))
        {
            ActiveScene = Crux.Engine.SetScene(new GameScene());
            return;
        }

        ActiveScene?.Update();
        ActiveScene?.OnSceneUpdateCallback?.Invoke();
    }
}

