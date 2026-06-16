using CruxEngine.Assets.Scenes;
using CruxEngine.Utilities.IO;
using Game.Assets.Scenes;

namespace Game;

public static class Game
{
    public static Showtime Current { get; internal set; } = null!;
    public static Scene ActiveScene => Current.ActiveScene!;
}

public class Showtime : GameClient
{  
    public Showtime()
    {
        Game.Current = this;
    }
    
    protected override void OnStart()
    {
        //Crux.Engine.SetScene(new DebugScene()); 
        //Crux.Engine.SetScene(new IslandScene()); 
        //Crux.Engine.SetScene(new PhysicsScene());
        Crux.Engine.SetScene(new ShowtimeScene()); 
    }
            
    protected override void OnUpdate()
    {
        
    }
}

