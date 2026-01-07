using CruxEngine.Assets.Scenes;
using CruxEngine.Utilities.IO;
using Game.Assets.Scenes;

namespace Game;

public static class Game
{
    public static Spectral Current { get; internal set; } = null!;
    public static Scene ActiveScene => Current.ActiveScene!;
}

public class Spectral : GameClient
{  
    public int Score = 0;

    public Spectral()
    {
        Game.Current = this;
    }
    
    protected override void OnStart()
    {
        //Crux.Engine.SetScene(new DebugScene()); 
        //Crux.Engine.SetScene(new IslandScene()); 
        Crux.Engine.SetScene(new PhysicsScene());

        //Crux.Engine.SetScene(new LandingScene()); 
    }
            
    protected override void OnUpdate()
    {
        
    }
}

