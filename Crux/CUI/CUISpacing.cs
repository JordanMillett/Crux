namespace Crux.CUI;

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

    public void Resolve(float parentWidth, float parentHeight)
    {
        Top.Resolve(parentHeight);
        Right.Resolve(parentWidth);
        Bottom.Resolve(parentHeight);
        Left.Resolve(parentWidth);
    }

    public float Horizontal => Left.Resolved + Right.Resolved;
    public float Vertical => Top.Resolved + Bottom.Resolved;
} 