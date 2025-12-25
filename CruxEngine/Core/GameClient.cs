namespace CruxEngine.Core;

public abstract class GameClient
{  
    public Scene ActiveScene { get => Crux.Engine.ActiveScene!; }
    
    public void Start()
    {
        Logger.Log("Game Client Loading...", LogSource.System);
        Crux.Engine.EngineUpdateEvent += Update;

        OnStart();

        Logger.Log("Game Client Started!", LogSource.System);
    }

    protected virtual void OnStart() {}
            
    protected void Update()
    {
        OnUpdate(); //Update Game Logic
        ActiveScene?.Update(); //Update Scene Logic
    }

    protected virtual void OnUpdate() {}
}

