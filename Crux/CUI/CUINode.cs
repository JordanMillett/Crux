using AngleSharp.Dom;

namespace Crux.CUI;

public struct CUIBounds
{
    public float? Height;
    public float? Width;

    public Vector2 Position; //Offset from parent top-left
}

public abstract class CUINode
{
    public CUIBounds Bounds;
    public List<CUINode> Children = [];
    public abstract void Render();
    public virtual void Update() {}
}

public class CUIContainer : CUINode
{
    public Color4? Background;

    public override void Render()
    {
        Logger.Log($"div");

        foreach (CUINode child in Children)
        {
            child.Render();
        }
    }
}

public class CUIText : CUINode
{
    public string? Text;

    public override void Render()
    {
        Logger.Log($"Text - {Text}");
    }
}