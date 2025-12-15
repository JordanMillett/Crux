using Game.Assets.Scenes;
using CruxEngine.Assets.Scenes;
using CruxEngine;

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
        Crux.Engine.OnEngineUpdateCallback += Update;

        
        //ActiveScene = Crux.Engine.SetScene(new IslandScene());   
        ActiveScene = Crux.Engine.SetScene(new DebugScene());  
        //ActiveScene = Crux.Engine.SetScene(new GameScene()); 

        Logger.Log("Game Started!", LogSource.System);
    }
            
    public void Update()
    {
        if (Input.IsActionPressed("restart scene"))
        {
            ActiveScene = Crux.Engine.SetScene(new DebugScene());
            return;
        }

        ActiveScene?.Update();
        ActiveScene?.OnSceneUpdateCallback?.Invoke();
    }
}

