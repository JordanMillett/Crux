using Crux.Components;

namespace Crux.CUI;

public struct CUIBounds
{
    public CUIUnit Width;
    public CUIUnit Height;
    
    public CUISpacing Margin;
    public CUISpacing Padding;

    public Vector2 RelativePosition;
    public Vector2 AbsolutePosition;
}

public struct CUISpacing
{
    public CUIUnit Left;
    public CUIUnit Right;
    public CUIUnit Top;
    public CUIUnit Bottom;

    public CUISpacing(CUIUnit allSides)
    {
        Left = Right = Top = Bottom = allSides;
    }

    public Vector2 HorizontalResolved => new(Left.Resolved + Right.Resolved, 0);
    public Vector2 VerticalResolved => new(0, Top.Resolved + Bottom.Resolved);
    public Vector2 TotalResolved => new(Left.Resolved + Right.Resolved, Top.Resolved + Bottom.Resolved);
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

        Bounds.Margin.Left.Resolve(availableWidth);
        Bounds.Margin.Right.Resolve(availableWidth);
        Bounds.Margin.Top.Resolve(availableHeight);
        Bounds.Margin.Bottom.Resolve(availableHeight);

        Bounds.Padding.Left.Resolve(availableWidth);
        Bounds.Padding.Right.Resolve(availableWidth);
        Bounds.Padding.Top.Resolve(availableHeight);
        Bounds.Padding.Bottom.Resolve(availableHeight);

        float maxChildWidth = 0;
        float totalChildHeight = 0;
        foreach (CUINode child in Children)
        {
            //Resolve
            child.Bounds.Margin.Left.Resolve(availableWidth);
            child.Bounds.Margin.Right.Resolve(availableWidth);
            child.Bounds.Margin.Top.Resolve(availableHeight);
            child.Bounds.Margin.Bottom.Resolve(availableHeight);

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