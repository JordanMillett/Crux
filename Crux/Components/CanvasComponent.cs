using Crux.CUI;
using Crux.Utilities.IO;
using OpenTK.Graphics.OpenGL4;

namespace Crux.Components;

public class CanvasComponent : RenderComponent
{          
    private CUINode? Root;
    
    public Dictionary<string, CUINode> NodesRefs = [];
    public Dictionary<string, Func<string>> BindPoints = [];

    public CanvasComponent(GameObject gameObject) : base(gameObject)
    {
        
    }
    
    public void ParseMarkup(string src)
    {
        NodesRefs.Clear();
        BindPoints.Clear();

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

        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        Root?.Render();
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
    }

    public Matrix4 GetModelMatrix(CUIBounds bounds)
    {
        return GetModelMatrix
        (
            bounds.Width.Resolved,
            bounds.Height.Resolved,
            bounds.AbsolutePosition.X,
            bounds.AbsolutePosition.Y
        );
    }

    public Matrix4 GetModelMatrix(float width, float height, float xpos, float ypos)
    {
        float screenWidth = GameEngine.Link.Resolution.X;
        float screenHeight = GameEngine.Link.Resolution.Y;
        float scaleX = width / screenWidth;
        float scaleY = height / screenHeight;

        Matrix4 scale = Matrix4.CreateScale(scaleX, scaleY, 1f);

        float ndcX = (xpos / GameEngine.Link.Resolution.X) * 2f - 1f;
        float ndcY = -((ypos / GameEngine.Link.Resolution.Y) * 2f - 1f);

        float offsetX = scaleX;
        float offsetY = -scaleY;

        Matrix4 translation = Matrix4.CreateTranslation(ndcX + offsetX, ndcY + offsetY, 0f);

        return scale * translation;
    }
}
