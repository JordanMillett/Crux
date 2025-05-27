using Crux.CUI;
using Crux.Utilities.IO;
using OpenTK.Graphics.OpenGL4;

namespace Crux.Components;

public class CanvasComponent : RenderComponent
{          
    private CUINode? Root;
    
    public Dictionary<string, Func<string>> BindPoints = [];

    public CanvasComponent(GameObject gameObject) : base(gameObject)
    {
        
    }
    
    public void ParseMarkup(string src)
    {
        var parser = new CUIParser(AssetHandler.ReadAssetInFull(src));
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
        
    }

    public void AfterRender()
    {        
        Root?.Measure();
        Root?.Arrange(Vector2.Zero);

        GL.DepthMask(false);
        Root?.Render();
        GL.DepthMask(true);
    }

    public Matrix4 GetModelMatrix(CUIBounds bounds)
    {
        float scaleX = bounds.Width / GameEngine.Link.Resolution.X;
        float scaleY = bounds.Height / GameEngine.Link.Resolution.Y;

        Matrix4 scale = Matrix4.CreateScale(scaleX, scaleY, 1f);

        float posX = (bounds.AbsolutePosition.X / GameEngine.Link.Resolution.X) * 2f - 1f;
        float posY = -((bounds.AbsolutePosition.Y / GameEngine.Link.Resolution.Y) * 2f - 1f);

        Matrix4 translation = Matrix4.CreateTranslation(posX, posY, 0f);

        return scale * translation;
    }

}
