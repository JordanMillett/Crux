using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Components;

namespace Crux.CUI;

public struct CUIBounds
{
    public float Height;
    public float Width;

    public Vector2 RelativePosition;
    public Vector2 AbsolutePosition;
}

public abstract class CUINode
{
    public CanvasComponent Canvas { get; init; }
    public CUIBounds Bounds;
    public List<CUINode> Children = [];

    public CUINode(CanvasComponent canvas)
    {
        Canvas = canvas;
    }

    public virtual void Measure() 
    {
        float maxWidth = 0;
        float totalHeight = 0;

        foreach (CUINode child in Children)
        {
            child.Measure();

            child.Bounds.RelativePosition = new Vector2(0, totalHeight);

            maxWidth = Math.Max(maxWidth, child.Bounds.Width);  
            totalHeight += child.Bounds.Height;          
        }

        Bounds.Width = maxWidth;
        Bounds.Height = totalHeight;
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