using CruxEngine.Components;

namespace CruxEngine.CUI;

public struct CUIBounds
{
    public CUIUnit Width;
    public CUIUnit Height;

    public CUISpacing Margin;
    public CUISpacing Padding;

    public Vector2 RelativePosition;
    public Vector2 AbsolutePosition;

    public CUILayoutMode LayoutMode;

    public bool Fixed; //ignore all other elements
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

    public string Identifier = "NULL";

    public CUINode(CanvasComponent canvas)
    {
        Canvas = canvas;
    }

    public void Output(float availableWidth, float availableHeight)
    {
        if(Debug.FlagEnabled("OutputCUINodes"))
        {
            if(!string.IsNullOrWhiteSpace(Identifier))
            {   
                Logger.Log(Identifier);
                Logger.Log($"Absolute Position: {Bounds.AbsolutePosition}");
                Logger.Log($"Width: {Bounds.Width.Resolved}px");
                Logger.Log($"Height: {Bounds.Height.Resolved}px");
                Logger.Log($"Available Width: {availableWidth}px");
                Logger.Log($"Available Height: {availableHeight}px");
                Logger.Log($"Layout: {Bounds.LayoutMode}");
                Logger.Log("");
            }
        }
    }
    
    public virtual void Measure(float availableWidth, float availableHeight)
    {   
        Bounds.Margin.Resolve(availableWidth, availableHeight);
        //Bounds.Padding.Resolve(availableWidth, availableHeight);

        //Logger.LogWarning(Bounds.Margin.Left.Resolved);
        
        Bounds.Width.Resolve(Bounds.LayoutMode == CUILayoutMode.Block, availableWidth, availableHeight);
        Bounds.Height.Resolve(false, availableWidth, availableHeight);
        /*
        availableWidth -= Bounds.Padding.HorizontalResolved;
        availableHeight -= Bounds.Padding.VerticalResolved;
        */
        float neededWidth = 0f;
        float neededHeight = 0f;

        foreach (CUINode child in Children)
        {
            child.Measure(availableWidth, availableHeight);

            neededHeight += child.Bounds.Height.Resolved + child.Bounds.Margin.VerticalResolved;
            neededWidth = Math.Max(neededWidth, child.Bounds.Width.Resolved + child.Bounds.Margin.HorizontalResolved);
        }

        Bounds.Width.Resolve(Bounds.LayoutMode == CUILayoutMode.Block, neededWidth, availableWidth);
        Bounds.Height.Resolve(false, neededHeight, availableHeight);

        Output(availableWidth, availableHeight);
    }

    public virtual void Arrange(Vector2 parentPosition)
    {
        Bounds.AbsolutePosition = parentPosition + Bounds.RelativePosition + new Vector2(Bounds.Margin.Left.Resolved, Bounds.Margin.Top.Resolved);

        float cursorX = 0;
        float cursorY = 0;

        //float cursorX = Bounds.Padding.Left.Resolved;
        //float cursorY = Bounds.Padding.Top.Resolved;

        foreach (CUINode child in Children)
        {
            child.Bounds.RelativePosition = new Vector2(cursorX, cursorY);
            cursorY += child.Bounds.Height.Resolved;
            child.Arrange(Bounds.AbsolutePosition);
        }
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