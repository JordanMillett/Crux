using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Components;

namespace Crux.CUI;

public struct CUIBounds
{
    public CUIUnit Width;
    public CUIUnit Height;

    public Vector2 RelativePosition;
    public Vector2 AbsolutePosition;
}

public enum CUIUnitType 
{
    Auto,
    Pixel,
    Percentage
}

public struct CUIUnit 
{
    public CUIUnitType Type { get; init; }
    public float Value { get; init; }
    public float Resolved { get; private set; }

    public CUIUnit(CUIUnitType type, float value = 0, float resolved = 0)
    {
        Value = value;
        Type = type;
        Resolved = resolved;
    }

    public static CUIUnit Parse(string input)
    {
        if (input == "auto") 
        {
            return new CUIUnit(CUIUnitType.Auto);
        }
        if (input.EndsWith("px")) 
        {
            if (float.TryParse(input[..^2], out float parsed))
                return new CUIUnit(CUIUnitType.Pixel, parsed);
        }
        if (input.EndsWith("%")) 
        {
            if (float.TryParse(input[..^1], out float parsed))
                return new CUIUnit(CUIUnitType.Percentage, parsed);
        }

        Logger.LogWarning($"Unknown CUI Unit Type in '{input}'.");
        return new CUIUnit(CUIUnitType.Pixel);
    }

    public void Resolve(float parentSize, float contentSize = 0f, bool Expands = false) //later support inline as well as block
    {
        Resolved = Type switch 
        {
            CUIUnitType.Pixel => Value,
            CUIUnitType.Percentage => parentSize * (Value / 100f),
            CUIUnitType.Auto => Expands ? parentSize : contentSize,
            _ => 0
        };
    }
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