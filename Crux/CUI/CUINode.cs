using Crux.Components;

namespace Crux.CUI;

public struct CUIBounds
{
    public CUIUnit Width;
    public CUIUnit Height;

    public Vector2 RelativePosition;
    public Vector2 AbsolutePosition;
}

public abstract class CUINode
{
    public CanvasComponent Canvas { get; init; }
    public CUINode? Parent { get; set; }
    public CUIBounds Bounds;
    public List<CUINode> Children = [];

    public CUINode(CanvasComponent canvas)
    {
        Canvas = canvas;
    }

    public virtual void Measure() 
    {
        float availableWidth = Parent?.Bounds.Width.Resolved ?? GameEngine.Link.Resolution.X;
        float availableHeight = Parent?.Bounds.Height.Resolved ?? GameEngine.Link.Resolution.Y;

        float maxChildWidth = 0;
        float totalChildHeight = 0;
        foreach (CUINode child in Children)
        {
            //Measure
            child.Measure();

            //Stack
            child.Bounds.RelativePosition = new Vector2(0, totalChildHeight);
            totalChildHeight += child.Bounds.Height.Resolved;

            //Expand
            maxChildWidth = Math.Max(maxChildWidth, child.Bounds.Width.Resolved);
        }

        //Resolve
        Bounds.Width.Resolve(availableWidth, maxChildWidth, true);
        Bounds.Height.Resolve(availableHeight, totalChildHeight);
    }

    public virtual void Arrange(Vector2 parentPosition)
    {
        Bounds.AbsolutePosition = parentPosition + Bounds.RelativePosition;

        foreach (CUINode child in Children)
            child.Arrange(Bounds.AbsolutePosition);
    }

    public virtual void Render()
    {
        foreach (CUINode child in Children)
            child.Render();
    }

    public virtual void Update() 
    {
        foreach (CUINode child in Children)
            child.Update();
    }
}