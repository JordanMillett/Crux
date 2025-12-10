using Game.Assets.Scenes;
using Crux.Assets.Scenes;

namespace Game;

public class GameInstance //MOVE INTO GAME ENGINE AND INHERET IT!
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
    
    Scene ActiveScene = null!;

    public GameInstance()
    {
        link = this;
    }
    
    public void Start()
    {
        Logger.Log("Game Loading...", LogSource.System);
        GameEngine.Link.OnEngineUpdateCallback += Update;

        
        ActiveScene = GameEngine.Link.SetScene(new IslandScene());   
        //ActiveScene = GameEngine.Link.SetScene(new DebugScene());  
        //ActiveScene = GameEngine.Link.SetScene(new GameScene()); 

        Logger.Log("Game Started!", LogSource.System);
    }
            
    public void Update()
    {
        if (Input.IsActionPressed("restart scene"))
        {
            ActiveScene = GameEngine.Link.SetScene(new GameScene());
            return;
        }

        ActiveScene?.Update();
        ActiveScene?.OnSceneUpdateCallback?.Invoke();
    }
}

