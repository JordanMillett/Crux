using Game.Assets.Scenes;
using Crux.Assets.Scenes;

namespace Game;

public class GameInstance
{
    private static GameInstance? link;
    public static GameInstance LINK
    {
        get
        {
            if (link == null)
                throw new InvalidOperationException("GameInstance is null");
            return link;
        }
    }
    
    public GameInstance()
    {
        link = this;
    }
    
    public void Ready()
    {
        Logger.Log("Game Loading...", LogSource.System);
        
        GameEngine.Link.OnUpdateCallback += Update;

        GameEngine.Link.ActiveScene = new IslandScene();
        //GameEngine.Link.ActiveScene = new DebugScene();
        //GameEngine.Link.ActiveScene = new GameScene();
        GameEngine.Link.ActiveScene.Start();

        Logger.Log($"Loaded Scene '{GameEngine.Link.ActiveScene.GetType().Name}'", LogSource.System);

        Logger.Log("Game Started!", LogSource.System);
    }
            
    public void Update()
    {
        GameEngine.Link.ActiveScene?.Update();
    }
}

