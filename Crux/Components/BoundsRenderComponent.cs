using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;

namespace Crux.Components;

public class BoundsRenderComponent : RenderComponent
{
    public static Shader shader { get; set; } = null!;

    //Instanced Data
    public Color4 AABBColor = Color4.Orange;
    public Color4 OBBColor = Color4.Blue;

    MeshBuffer meshBuffer;

    public static List<BoundsRenderComponent> Instances = [];
    
    public MeshBoundsColliderComponent Source;

    public BoundsRenderComponent(GameObject gameObject): base(gameObject)
    {
        if (shader == null)
            shader = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Instance_Outline);

        meshBuffer = GraphicsCache.GetInstancedLineBuffer("LineBounds", Shapes.LineBounds);
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
        BoundsRenderComponent clone = new BoundsRenderComponent(gameObject);

        return clone;
    }

    public override void Render()
    { 
        if (!meshBuffer.DrawnThisFrame)
        {
            int packIndex = 0;
            int totalInstances = Instances.Count * 2;
            
            float[] flatpack = new float[totalInstances *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4))
            )];

            foreach(BoundsRenderComponent instance in Instances)
            {
                if(instance.Source == null || instance.Source.OBBAxes == null)
                {
                    totalInstances -= 2;
                    continue;
                }        

                //AABB
                Vector3 middle = (instance.Source.AABBMin + instance.Source.AABBMax) * 0.5f;
                Vector3 size = instance.Source.AABBMax - instance.Source.AABBMin;
                Matrix4 BoundsModelMatrix = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(middle);

                MatrixHelper.Matrix4ToArray(BoundsModelMatrix, out float[] values);
                for(int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];

                flatpack[packIndex++] = instance.AABBColor.R;
                flatpack[packIndex++] = instance.AABBColor.G;
                flatpack[packIndex++] = instance.AABBColor.B;
                flatpack[packIndex++] = instance.AABBColor.A;
                
                //OBB
                Matrix4 scaleMatrix = Matrix4.CreateScale(instance.Source.OBBHalfExtents * 2.0f);
                Matrix4 rotationMatrix = MatrixHelper.CreateRotationMatrixFromAxes(instance.Source.OBBAxes);
                Matrix4 translationMatrix = Matrix4.CreateTranslation(instance.Source.OBBCenter);
                BoundsModelMatrix = scaleMatrix * rotationMatrix * translationMatrix;

                MatrixHelper.Matrix4ToArray(BoundsModelMatrix, out values);
                for(int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];
                
                flatpack[packIndex++] = instance.OBBColor.R;
                flatpack[packIndex++] = instance.OBBColor.G;
                flatpack[packIndex++] = instance.OBBColor.B;
                flatpack[packIndex++] = instance.OBBColor.A;
            }

            meshBuffer.SetDynamicVBOData(flatpack, totalInstances);
            
            shader.Bind();
            
            meshBuffer.DrawLinesInstanced(Shapes.LineBounds.Length, totalInstances);

            shader.Unbind();

            meshBuffer.DrawnThisFrame = true;
        }
    }
}