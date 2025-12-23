namespace Game;

public static class Game
{
    public static GameClient Client { get; internal set; } = null!;
    public static MyGame Current { get; internal set; } = null!;
    public static Scene ActiveScene => Client.ActiveScene!;
}

public abstract class GameClient
{  
    public Scene ActiveScene { get; internal set; } = null!;
    
    public GameClient()
    {
        Game.Client = this;
    }

    public void Start()
    {
        Logger.Log("Game Client Loading...", LogSource.System);
        Crux.Engine.OnEngineUpdateCallback += Update;

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

