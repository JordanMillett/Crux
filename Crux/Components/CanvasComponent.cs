using Crux.UI;
using Crux.UI.CUI;

namespace Crux.Components;

public class CanvasComponent : Component
{          
    public string CUIMarkup =
    @"
    <div>
        <p>Hello!</p>
    </div>
    ";

    private List<UIObject> Nodes = new List<UIObject>();

    public CanvasComponent(GameObject gameObject) : base(gameObject)
    {
        ParseMarkup();
    }
    
    public void ParseMarkup()
    {
        var parser = new CUIParser(CUIMarkup);
        UIObject? root = parser.Parse();

        Nodes.Clear();

        if (root != null)
            Nodes.Add(root);
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
        foreach (var node in Nodes)
        {
            // Your logic here, e.g., update layout, animations, etc.
        }
    }
}
