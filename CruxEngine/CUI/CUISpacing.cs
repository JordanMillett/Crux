namespace CruxEngine.CUI;

public struct CUISpacing
{
    public CUIUnit Top;
    public CUIUnit Right;
    public CUIUnit Bottom;
    public CUIUnit Left;

    public CUISpacing(CUIUnitType type)
    {
        Top = new CUIUnit(type);
        Right = new CUIUnit(type);
        Bottom = new CUIUnit(type);
        Left = new CUIUnit(type);
    }

    public CUISpacing(CUIUnit top, CUIUnit right, CUIUnit bottom, CUIUnit left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public void Resolve(float availableWidth, float availableHeight)
    {
        Top.Resolve(false, 0, availableHeight);
        Right.Resolve(false, 0, availableWidth);
        Bottom.Resolve(false, 0, availableHeight);
        Left.Resolve(false, 0, availableWidth);
    }

    public readonly float HorizontalResolved => Left.Resolved + Right.Resolved;
    public readonly float VerticalResolved => Top.Resolved + Bottom.Resolved;
} 
