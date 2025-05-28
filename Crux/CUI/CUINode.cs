using Crux.Components;

namespace Crux.CUI;

public struct CUIBounds
{
    public CUIUnit Width;
    public CUIUnit Height;

    public CUISpacing Padding;

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
        float parentWidth = Parent == null ? GameEngine.Link.Resolution.X
            : (Parent.Bounds.Width.Resolved - Parent.Bounds.Padding.Horizontal);

        float parentHeight = Parent == null ? GameEngine.Link.Resolution.Y
            : (Parent.Bounds.Height.Resolved - Parent.Bounds.Padding.Vertical);
        
        Bounds.Padding.Resolve(parentWidth, parentHeight);

        float rightExtents = 0;
        float bottomExtents = 0;
        foreach (CUINode child in Children)
        {
            //Measure
            child.Measure();

            //Stack
            child.Bounds.RelativePosition = new Vector2(Bounds.Padding.Left.Resolved, Bounds.Padding.Top.Resolved + bottomExtents);
            bottomExtents += child.Bounds.Height.Resolved;

            //Expand
            rightExtents = Math.Max(child.Bounds.RelativePosition.X + child.Bounds.Width.Resolved, rightExtents);
        }

        float totalContentWidth = rightExtents;
        float maxAllowedWidth = parentWidth;

        //Resolve
        Bounds.Width.Resolve(maxAllowedWidth, totalContentWidth, true);
        Bounds.Height.Resolve(parentHeight, bottomExtents + Bounds.Padding.Vertical);
    }

    public virtual void Arrange(Vector2 parentPosition)
    {
        Bounds.AbsolutePosition = parentPosition + Bounds.RelativePosition;

        foreach (var child in Children)
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