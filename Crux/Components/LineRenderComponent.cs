using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;

namespace Crux.Components;

public class LineRenderComponent : RenderComponent
{
    public static Shader shader { get; set; } = null!;

    public Color4 Color = Color4.Red;

    MeshBuffer meshBuffer;

    public LineRenderComponent(GameObject gameObject): base(gameObject)
    {

        if (shader == null)
        {
            shader = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Outline);
        }

        meshBuffer = GraphicsCache.GetInstancedLineBuffer("LineAnchor", Shapes.LineAnchor);
    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        LineRenderComponent clone = new LineRenderComponent(gameObject);

        return clone;
    }

    public override void Render()
    {   
        if (!meshBuffer.DrawnThisFrame)
        {

            int instances = 1;

            float[] flatpack = new float[instances *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4))
            )];

            int packIndex = 0;
            MatrixHelper.Matrix4ToArray(GameObject.Transform.ModelMatrix, out float[] values);
            for(int j = 0; j < values.Length; j++)
                flatpack[packIndex++] = values[j];

            flatpack[packIndex++] = Color.R;
            flatpack[packIndex++] = Color.G;
            flatpack[packIndex++] = Color.B;
            flatpack[packIndex++] = Color.A;

            meshBuffer.SetDynamicVBOData(flatpack, instances);
            
            shader.Bind();
            
            meshBuffer.DrawLinesInstanced(Shapes.LineAnchor.Length, 1);

            shader.Unbind();
        }
    }
}
