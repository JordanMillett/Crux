using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using System.Diagnostics;

namespace Crux.Components;

public class LineRenderComponent : RenderComponent
{
    public static Shader shader { get; set; } = null!;

    //Instanced Data
    public Color4 Color = Color4.Red;

    MeshBuffer meshBuffer;

    public static List<LineRenderComponent> Instances = [];

    public LineRenderComponent(GameObject gameObject): base(gameObject)
    {
        if (shader == null)
            shader = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Outline, "", true);

        meshBuffer = GraphicsCache.GetInstancedLineBuffer("LineAnchor", Shapes.LineAnchor);
        Instances.Add(this);
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
            float[] flatpack = new float[Instances.Count *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4))
            )];

            int packIndex = 0;

            foreach(LineRenderComponent instance in Instances)
            {
                MatrixHelper.Matrix4ToArray(instance.Transform.ModelMatrix, out float[] values);
                for(int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];

                flatpack[packIndex++] = instance.Color.R;
                flatpack[packIndex++] = instance.Color.G;
                flatpack[packIndex++] = instance.Color.B;
                flatpack[packIndex++] = instance.Color.A;
            }

            meshBuffer.SetDynamicVBOData(flatpack, Instances.Count);
            
            shader.Bind();
            
            meshBuffer.DrawLinesInstanced(Shapes.LineAnchor.Length, Instances.Count);

            shader.Unbind();

            meshBuffer.DrawnThisFrame = true;
        }
    }
}
