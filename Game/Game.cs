using CruxEngine.Assets.Scenes;
using Game.Assets.Scenes;

namespace Game;

public class GameInstance
{  
    Scene ActiveScene = null!;
    
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

