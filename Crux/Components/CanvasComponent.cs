using Crux.CUI;

namespace Crux.Components;

public class CanvasComponent : RenderComponent
{          
    public string CUIMarkup =
    @"
    <div>
        <p>Hello!</p>
    </div>
    ";

    private CUINode Root = null!;

    public Vector2 VirtualResolution = new Vector2(1280, 720);

    public CanvasComponent(GameObject gameObject) : base(gameObject)
    {
        ParseMarkup();
    }
    
    public void ParseMarkup()
    {
        CUIParser.canvas = this; //Must do!
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
        
    }

    public override void Render()
    {
        Root.Measure();
        Root.Render();
    }

    public Matrix4 GetVirtualScale(float width, float height)
    {
        float scaleX = 2f / VirtualResolution.X;
        float scaleY = 2f / VirtualResolution.Y;

        return Matrix4.CreateScale(width * scaleX, height * scaleY, 1.0f);
    }

    public Matrix4 GetVirtualTranslation(float x, float y, float width, float height)
    {
        float scaleX = 2f / VirtualResolution.X;
        float scaleY = 2f / VirtualResolution.Y;

        // Translate to the center of the quad
        float translatedX = x * scaleX - 1.0f + (width * scaleX * 0.5f);
        float translatedY = -(y * scaleY - 1.0f + (height * scaleY * 0.5f)); // flip Y if top-left origin

        return Matrix4.CreateTranslation(translatedX, translatedY, 0.0f);
    }
}
