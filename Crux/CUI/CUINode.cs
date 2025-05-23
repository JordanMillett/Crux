using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;

namespace Crux.CUI;

public struct CUIBounds
{
    public float Height;
    public float Width;

    public Vector2 Position; //Offset from parent top-left
}

public abstract class CUINode
{
    public CUIBounds Bounds;
    public List<CUINode> Children = [];

    public abstract void Measure();
    public abstract void Render();

    public virtual void Update() {}
}

public class CUIContainer : CUINode
{
    private static Shader? ShaderSingleton { get; set; }

    //Instanced Data
    public Color4 Background = Color4.White;

    private readonly MeshBuffer meshBuffer;

    public static List<CUIContainer> Instances = [];

    public CUIContainer()
    {
        if (ShaderSingleton == null)
            ShaderSingleton = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.UI, true, "");

        meshBuffer = GraphicsCache.GetInstancedUIBuffer(true);
        Instances.Add(this);
    }

    public override void Measure()
    {
        float maxWidth = 0;
        float totalHeight = 0;

        foreach (CUINode child in Children)
        {
            child.Measure();

            child.Bounds.Position = new Vector2(0, totalHeight);

            maxWidth = Math.Max(maxWidth, child.Bounds.Width);  
            totalHeight += child.Bounds.Height;          
        }

        Bounds.Width = maxWidth;
        Bounds.Height = totalHeight;
    }

    public override void Render()
    {
        if (!meshBuffer.DrawnThisFrame)
        {
            float[] flatpack = new float[Instances.Count *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4))
            )];

            int packIndex = 0;

            foreach(CUIContainer instance in Instances)
            {
                Matrix4 modelMatrix = 
                    Matrix4.CreateScale(1f, 1f, 1.0f) *
                    Matrix4.CreateTranslation(0.5f, 0.5f, 0.0f); //todo set positions with margins and such

                MatrixHelper.Matrix4ToArray(modelMatrix, out float[] values);
                for(int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];

                flatpack[packIndex++] = instance.Background.R;
                flatpack[packIndex++] = instance.Background.G;
                flatpack[packIndex++] = instance.Background.B;
                flatpack[packIndex++] = instance.Background.A;
            }

            meshBuffer.SetDynamicVBOData(flatpack, Instances.Count);
            
            ShaderSingleton?.Bind();
            
            meshBuffer.DrawInstancedWithoutIndices(Shapes.QuadVertices.Length, Instances.Count);

            ShaderSingleton?.Unbind();

            meshBuffer.DrawnThisFrame = true;

            Logger.LogWarning($"{Bounds.Width} , {Bounds.Height}");
        }

        foreach (CUINode child in Children)
            child.Render();
    }
}

public class CUIText : CUINode
{
    public string Text { get; set; } = "";

    public override void Measure()
    {
        Bounds.Width = Text.Length * 8;
        Bounds.Height = 16;
    }

    public override void Render()
    {
        
    }
}