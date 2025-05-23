using Crux.CUI;

namespace Crux.Components;

public class CanvasComponent : Component
{          
    public string CUIMarkup =
    @"
    <div>
        <p>Hello!</p>
    </div>
    ";

    private CUINode Root = null!;

    public CanvasComponent(GameObject gameObject) : base(gameObject)
    {
        ParseMarkup();
    }
    
    public void ParseMarkup()
    {
        var parser = new CUIParser(CUIMarkup);
        Root = parser.Parse()!;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        return new CanvasComponent(gameObject);
    }

    public override void Update()
    {
        Root.Render();
    }
}
