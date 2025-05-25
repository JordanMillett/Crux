using Crux.CUI;
using Crux.Utilities.IO;

namespace Crux.Components;

public class CanvasComponent : RenderComponent
{          
    private CUINode? Root;

    public Vector2 VirtualResolution = new Vector2(1280, 720);

    public CanvasComponent(GameObject gameObject) : base(gameObject)
    {
        ParseMarkup();
    }
    
    public void ParseMarkup()
    {
        var parser = new CUIParser(AssetHandler.ReadAssetInFull("Game/Assets/CUI/main.html"));
        Root = parser.Parse(this)!;
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
        Root?.Update();
    }

    public override void Render()
    {
        Root?.Measure();
        Root?.Arrange(Vector2.Zero);
        Root?.Render();
    }

    public Matrix4 GetModelMatrix(CUIBounds bounds)
    {
        float scaleX = bounds.Width / VirtualResolution.X;
        float scaleY = bounds.Height / VirtualResolution.Y;
        Matrix4 scale = Matrix4.CreateScale(scaleX, scaleY, 1f);

        float posX = ((bounds.AbsolutePosition.X + bounds.Width / 2f) / VirtualResolution.X) * 2f - 1f;
        float posY = -(((bounds.AbsolutePosition.Y + bounds.Height / 2f) / VirtualResolution.Y) * 2f - 1f);
        Matrix4 translation = Matrix4.CreateTranslation(posX, posY, 0f);

        return scale * translation;
    }

    public Matrix4 GetLetterModelMatrix(CUIBounds bounds)
    {
        float width = bounds.Width / VirtualResolution.X;
        float height = 2f * width;

        Matrix4 scale = Matrix4.CreateScale(width, height, 1f);

        float posX = ((bounds.AbsolutePosition.X + bounds.Width / 2f) / VirtualResolution.X) * 2f - 1f;
        float posY = -(((bounds.AbsolutePosition.Y + bounds.Height / 2f) / VirtualResolution.Y) * 2f - 1f);

        Matrix4 translation = Matrix4.CreateTranslation(posX, posY, 0f);

        return scale * translation;
    }
}
