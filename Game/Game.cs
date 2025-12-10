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

        //Scene ChosenScene = new IslandScene();
        Scene ChosenScene = new DebugScene();
        //Scene ChosenScene = new GameScene();

        ActiveScene = GameEngine.Link.SetScene(new DebugScene());  

        Logger.Log("Game Started!", LogSource.System);
    }
            
    public void Update()
    {
        if (Input.IsActionPressed("restart scene"))
        {
            ActiveScene = GameEngine.Link.SetScene(new DebugScene());
            return;
        }

        ActiveScene?.Update();
        ActiveScene?.OnSceneUpdateCallback?.Invoke();
    }
}

