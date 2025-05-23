namespace Crux.UI;

public struct UIBounds
{
    public float? Height;
    public float? Width;

    public Vector2 Position; //Offset from parent top-left
}

public abstract class UINode
{
    public UIBounds Bounds;
    public List<UINode> Children = [];
    public abstract void Render();
    public virtual void Update() {}
}

public class UIContainer : UINode
{
    public Color4? Background;

    public override void Render()
    {

    }
}

public class UIText : UINode
{
    public string? Text;

    public override void Render()
    {

    }
}