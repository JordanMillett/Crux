using CruxEngine;

namespace Game.Logic;

public class Enemy : Component
{      
    public float force = 10f;

    public Enemy(GameObject gameObject) : base(gameObject)
    {

    }
    
    //MAKE THIS BETTER TOO THIS IS WRONG, DONT NEED THIS MUCH BOILERPLATE
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    //DO SOMETHING BETTER WITH THIS
    public override Component Clone(GameObject gameObject)
    {
        return new Enemy(gameObject);
    }

    //ACTIVE SCENE PLAYER GET THAT INFO EASILY THROUGH CRUX.activescene.player
    //CROSShair CUI align center show image
    public override void Update()
    {
        //fixed timestep?
        //Crux.Engine.fixedDeltaTime
        this.Transform.WorldPosition += Vector3.Normalize((Crux.Camera.Transform.WorldPosition - this.Transform.WorldPosition)) * 0.025f;
    }
}
