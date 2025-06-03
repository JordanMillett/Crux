using Crux.Components;

namespace Crux.CUI;

public struct CUIBounds
{
    public CUIUnit Width;
    public CUIUnit Height;

    public CUISpacing Padding;

    public Vector2 RelativePosition;
    public Vector2 AbsolutePosition;

    public CUILayoutMode LayoutMode;
}

public enum CUILayoutMode
{
    Block,
    InlineBlock
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

    public Vector2 GetAvailableSpace()
    {
        float parentWidth;
        float parentHeight;

        if(Parent == null)
        {
            parentWidth = GameEngine.Link.Resolution.X;
            parentHeight = GameEngine.Link.Resolution.Y;

        }else
        {
            parentWidth = Parent.Bounds.Width.Resolved - Parent.Bounds.Padding.Horizontal;
            parentHeight = Parent.Bounds.Height.Resolved - Parent.Bounds.Padding.Vertical;
        }

        return new Vector2(parentWidth, parentHeight);
    }

    public virtual void Measure() 
    {
        Vector2 availableSpace = GetAvailableSpace();
        //Bounds.Padding.Resolve(availableSpace.X, availableSpace.Y);
        
        float totalContentWidth = 0;
        float totalContentHeight = 0;
        foreach (CUINode child in Children)
        {
            //Measure
            child.Measure();

            //Stack
            child.Bounds.RelativePosition = new Vector2(Bounds.Padding.Left.Resolved, Bounds.Padding.Top.Resolved + totalContentHeight);
            totalContentHeight += child.Bounds.Height.Resolved;

            //Expand
            totalContentWidth = Math.Max(child.Bounds.RelativePosition.X + child.Bounds.Width.Resolved, totalContentWidth);
        }

        //Resolve
        Bounds.Width.Resolve(availableSpace.X, totalContentWidth, 0f, false);
        Bounds.Height.Resolve(availableSpace.Y, totalContentHeight, 0f, false);
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