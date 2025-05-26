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
        float aspectDifference = Math.Min(GameEngine.Link.Resolution.X / VirtualResolution.X, GameEngine.Link.Resolution.Y / VirtualResolution.Y);

        float scaledWidth = VirtualResolution.X * aspectDifference;
        float scaledHeight = VirtualResolution.Y * aspectDifference;

        float offsetX = (GameEngine.Link.Resolution.X - scaledWidth) / 2f;
        float offsetY = (GameEngine.Link.Resolution.Y - scaledHeight) / 2f;

        float normalizedOffsetX = (offsetX / GameEngine.Link.Resolution.X) * 2f;
        float normalizedOffsetY = (offsetY / GameEngine.Link.Resolution.Y) * 2f;

        float normScaleX = (bounds.Width / VirtualResolution.X) * aspectDifference;
        float normScaleY = (bounds.Height / VirtualResolution.Y) * aspectDifference;

        Matrix4 scale = Matrix4.CreateScale(normScaleX, normScaleY, 1f);

        float posX = ((bounds.AbsolutePosition.X / VirtualResolution.X) * 2f - 1f) * aspectDifference + normalizedOffsetX - 1f + aspectDifference;
        float posY = -(((bounds.AbsolutePosition.Y / VirtualResolution.Y) * 2f - 1f) * aspectDifference + normalizedOffsetY - 1f + aspectDifference);

        Matrix4 originShift = Matrix4.CreateTranslation(1f, -1f, 0f);
        Matrix4 translation = Matrix4.CreateTranslation(posX, posY, 0f);

        return originShift * scale * translation;
    }
}
