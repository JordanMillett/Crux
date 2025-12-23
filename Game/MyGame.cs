using CruxEngine.Assets.Scenes;
using Game.Assets.Scenes;

namespace Game;

public class MyGame : GameClient
{  
    public int Score = 0;

    public MyGame()
    {
        Game.Current = this;
    }
    
    protected override void OnStart()
    {
        //ActiveScene = Crux.Engine.SetScene(new IslandScene());   
        //ActiveScene = Crux.Engine.SetScene(new DebugScene());  
        ActiveScene = Crux.Engine.SetScene(new GameScene()); 
    }
            
    protected override void OnUpdate()
    {
        
    }
}

