using CruxEngine.Components;

namespace CruxEngine.CUI;

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

    public string Identifier = "NULL";

    public CUINode(CanvasComponent canvas)
    {
        Canvas = canvas;
    }

    /*
    public Vector2 GetAvailableSpace()
    {
        if (Parent == null)
            return new Vector2(Crux.Engine.Resolution.X, Crux.Engine.Resolution.Y);

        // InlineBlock does NOT constrain children
        if (Parent.Bounds.LayoutMode == CUILayoutMode.InlineBlock)
            return Parent.GetAvailableSpace();

        return new Vector2(Parent.Bounds.Width.Resolved, Parent.Bounds.Height.Resolved);
    }
    */

    /*
    public Vector2 GetAvailableSpace()
    {
        CUINode? node = Parent;
        while (node != null)
        {
            if (node.Bounds.LayoutMode == CUILayoutMode.Block)
            {
                return new Vector2(
                    node.Bounds.Width.Resolved,
                    node.Bounds.Height.Resolved
                );
            }

            node = node.Parent;
        }

        return new Vector2(
            Crux.Engine.Resolution.X,
            Crux.Engine.Resolution.Y
        );
    }
    */
    /*
    public Vector2 GetAvailableSpace() //MODIFY THIS TO BE THE PARENT OF THE PARENT IF IT IS INLINE BLOCK
    {
        //return new Vector2(Crux.Engine.Resolution.X, Crux.Engine.Resolution.Y); //REMOVE

        float availableWidth;
        float availableHeight;

        //GET PARENT OF PARENT
        //if(Parent == null || (Parent.Bounds.LayoutMode == CUILayoutMode.Block))

        if(Parent == null)
        {
            availableWidth = Crux.Engine.Resolution.X;
            availableHeight = Crux.Engine.Resolution.Y;
            return new Vector2(availableWidth, availableHeight);
        }
        
        if(Parent.Bounds.LayoutMode == CUILayoutMode.Block)
        {
            availableWidth = Parent.Bounds.Width.Resolved - Parent.Bounds.Padding.Horizontal;
            availableHeight = Parent.Bounds.Height.Resolved - Parent.Bounds.Padding.Vertical;
        }else
        {
            availableWidth = 200;
            availableHeight = 200;
            //Vector2 availableSpace = Parent.GetAvailableSpace();
            //availableWidth = availableSpace.X;
            //availableHeight = availableSpace.Y;
        }

        return new Vector2(availableWidth, availableHeight);
    }
    */

    public void Output(Vector2 availableSpace)
    {
        return;

        if(!string.IsNullOrWhiteSpace(Identifier))
        {   
            Logger.Log(Identifier);
            Logger.Log($"Absolute Position: {Bounds.AbsolutePosition}");
            Logger.Log($"Width: {Bounds.Width.Resolved}px");
            Logger.Log($"Height: {Bounds.Height.Resolved}px");
            Logger.Log($"Available Width: {availableSpace.X}px");
            Logger.Log($"Available Height: {availableSpace.Y}px");
            Logger.Log($"Layout: {Bounds.LayoutMode}");
            Logger.Log("");
        }
    }
    
    public virtual void Measure(Vector2 availableSpace)
    {   
        Bounds.Width.Resolve(availableSpace.X, 0f, 0f, Bounds.LayoutMode == CUILayoutMode.Block);
        Bounds.Height.Resolve(availableSpace.Y, 0f, 0f, false);

        float contentWidth = 0f;
        float contentHeight = 0f;

        Vector2 childAvailableSpace = new Vector2(Bounds.Width.Resolved, Bounds.Height.Resolved);

        foreach (CUINode child in Children)
        {
            child.Measure(childAvailableSpace);

            contentHeight += child.Bounds.Height.Resolved;
            contentWidth = Math.Max(contentWidth, child.Bounds.Width.Resolved);
        }

        if (Bounds.Width.Type == CUIUnitType.Auto)
            Bounds.Width.Resolve(availableSpace.X, contentWidth, 0f, Bounds.LayoutMode == CUILayoutMode.Block);
        if (Bounds.Height.Type == CUIUnitType.Auto)
            Bounds.Height.Resolve(availableSpace.Y, contentHeight, 0f, false);

        Output(availableSpace);
    }


    /*
    public virtual void Measure() 
    {
        Vector2 availableSpace = GetAvailableSpace();
        //Logger.Log($"{GetType().Name} - {Children.Count}x Children - {availableSpace.X}, {availableSpace.Y}");
        //Bounds.Padding.Resolve(availableSpace.X, availableSpace.Y);

        Output(availableSpace);

        //PREMEASURE UNBOUND SPACE, TOTAL POSSIBLE THAT CAN BE USED, AND THEN TRY AGAIN????
        
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
        Bounds.Width.Resolve(availableSpace.X, totalContentWidth, 0f, Bounds.LayoutMode == CUILayoutMode.Block);
        Bounds.Height.Resolve(availableSpace.Y, totalContentHeight, 0f, false);
    }
    */

    /*
    public virtual void Arrange(Vector2 parentPosition)
    {
        Bounds.AbsolutePosition = parentPosition + Bounds.RelativePosition;

        foreach (var child in Children)
            child.Arrange(Bounds.AbsolutePosition);
    }
    */

    public virtual void Arrange(Vector2 parentPosition)
    {
        Bounds.AbsolutePosition = parentPosition + Bounds.RelativePosition;

        float cursorY = 0f;

        foreach (CUINode child in Children)
        {
            child.Bounds.RelativePosition = new Vector2(0f, cursorY);
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