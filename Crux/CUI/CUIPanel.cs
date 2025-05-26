using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Components;

namespace Crux.CUI;

public class CUIPanel : CUINode
{
    private static Shader? ShaderSingleton { get; set; }
    private readonly MeshBuffer meshBuffer;

    //Instanced Data
    public Color4 Background = Color4.White;

    public static List<CUIPanel> Instances = [];

    public CUIPanel(CanvasComponent canvas): base(canvas)
    {
        if (ShaderSingleton == null)
            ShaderSingleton = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Unlit_2D, true, "");

        meshBuffer = GraphicsCache.GetInstancedQuadBuffer("CUIPanel");
        Instances.Add(this);
    }

    public override void Render()
    {
        if (!meshBuffer.DrawnThisFrame)
        {
            float[] flatpack = new float[Instances.Count *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector2))
            )];

            int packIndex = 0;
            foreach(CUIPanel instance in Instances)
            {
                Matrix4 modelMatrix = Canvas.GetModelMatrix(instance.Bounds);

                //PACK
                MatrixHelper.Matrix4ToArray(modelMatrix, out float[] values);
                for(int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];

                flatpack[packIndex++] = instance.Background.R;
                flatpack[packIndex++] = instance.Background.G;
                flatpack[packIndex++] = instance.Background.B;
                flatpack[packIndex++] = instance.Background.A;

                packIndex += 2;

                //Logger.Log($"Container: {instance.Bounds.Width} x {instance.Bounds.Height} at ({instance.Bounds.AbsolutePosition.X}, {instance.Bounds.AbsolutePosition.Y})");
            }

            meshBuffer.SetDynamicVBOData(flatpack, Instances.Count);
            
            ShaderSingleton?.Bind();
            
            meshBuffer.DrawInstancedWithoutIndices(Shapes.QuadVertices.Length, Instances.Count);

            ShaderSingleton?.Unbind();

            meshBuffer.DrawnThisFrame = true;
        }

        base.Render();
    }
}