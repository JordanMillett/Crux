namespace Crux.CUI;

public enum CUIUnitType 
{
    Auto,
    Pixel,
    Percentage,
    ViewportWidth,
    ViewportHeight,
    Em   
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

    public const float DefaultFontSizePixels = 16f;
    public static CUIUnit DefaultFontSize => new CUIUnit(CUIUnitType.Pixel, DefaultFontSizePixels);

    public static CUIUnit Parse(string input)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            return new CUIUnit(CUIUnitType.Auto);

        if (input == "auto") 
            return new CUIUnit(CUIUnitType.Auto);
        
        if (input.EndsWith("px") && float.TryParse(input[..^2], out float px))
            return new CUIUnit(CUIUnitType.Pixel, px);
        
        if (input.EndsWith('%') && float.TryParse(input[..^1], out float percent))
            return new CUIUnit(CUIUnitType.Percentage, percent);
        
        if (input.EndsWith("vw") && float.TryParse(input[..^2], out float vw))
            return new CUIUnit(CUIUnitType.ViewportWidth, vw);

        if (input.EndsWith("vh") && float.TryParse(input[..^2], out float vh))
            return new CUIUnit(CUIUnitType.ViewportHeight, vh);

        if (input.EndsWith("em") && float.TryParse(input[..^2], out float em))
            return new CUIUnit(CUIUnitType.Em, em);

        Logger.LogWarning($"Unknown CUI Unit Type in '{input}'.");
        return new CUIUnit(CUIUnitType.Auto);
    }

    public void Resolve(float parentSize, float contentSize, bool Expands)
    {
        Resolve(parentSize, contentSize, DefaultFontSizePixels, Expands);
    }

    public void Resolve(float parentSize, float contentSize = 0f, float fontSize = DefaultFontSizePixels, bool Expands = false)
    {
        Resolved = Type switch 
        {
            CUIUnitType.Pixel => Value * GameEngine.Link.DpiMultiplier,                     //Pixel based, must be scaled
            CUIUnitType.Percentage => parentSize * (Value / 100f),
            CUIUnitType.Auto => Expands ? parentSize : contentSize,
            CUIUnitType.ViewportWidth => GameEngine.Link.Resolution.X * (Value / 100f),
            CUIUnitType.ViewportHeight => GameEngine.Link.Resolution.Y * (Value / 100f),
            CUIUnitType.Em => fontSize * Value * GameEngine.Link.DpiMultiplier,             //Pixel based, must be scaled
            _ => 0
        };
    }
}