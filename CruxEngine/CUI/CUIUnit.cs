namespace CruxEngine.CUI;

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
    public float Unresolved { get; set; }
    public float Resolved { get; private set; } //Always pixels

    public readonly float ResolvedPixels { get { return Unresolved * Crux.Engine.DpiMultiplier; } }

    public CUIUnit(CUIUnitType type, float uresolved = 0, float resolved = 0)
    {
        Type = type;
        Unresolved = uresolved;
        Resolved = resolved;
    }

    //public const float DefaultFontSizePixels = 16f;
    //public static CUIUnit DefaultFontSize => new CUIUnit(CUIUnitType.Pixel, DefaultFontSizePixels);

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

    public void Resolve(bool stretchToFill, float neededSpace = 0f, float availableSpace = 0f)
    {
        Resolved = Type switch 
        {
            CUIUnitType.Pixel => ResolvedPixels,   
            CUIUnitType.Auto => stretchToFill ? availableSpace : neededSpace,                            
            CUIUnitType.Percentage => availableSpace * (Unresolved / 100f),
            CUIUnitType.ViewportWidth => Crux.Engine.Resolution.X * (Unresolved / 100f),
            CUIUnitType.ViewportHeight => Crux.Engine.Resolution.Y * (Unresolved / 100f),
            CUIUnitType.Em => neededSpace * ResolvedPixels,       
            _ => 0
        };   
    }

    /*
    public void Resolve(float availableSpace, float contentSize = 0f, float fontSize = 0f, bool fillAvailableSpace = false)
    {
        Resolved = Type switch 
        {
            CUIUnitType.Pixel => Value * Crux.Engine.DpiMultiplier,                     //Pixel based, must be scaled
            CUIUnitType.Percentage => availableSpace * (Value / 100f),
            CUIUnitType.Auto => fillAvailableSpace ? availableSpace : contentSize,
            CUIUnitType.ViewportWidth => Crux.Engine.Resolution.X * (Value / 100f),
            CUIUnitType.ViewportHeight => Crux.Engine.Resolution.Y * (Value / 100f),
            CUIUnitType.Em => fontSize * Value * Crux.Engine.DpiMultiplier,             //Pixel based, must be scaled
            _ => 0
        };
    }
    */
}