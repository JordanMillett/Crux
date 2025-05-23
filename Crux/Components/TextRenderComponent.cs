using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;

namespace Crux.Components;

public class TextRenderComponent : RenderComponent
{     
    public MeshBuffer FontBuffer;

    private readonly string Charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
    
    private static Shader? fontMaterial;
    public string Text = "";
    public Vector2 StartPosition = Vector2.Zero;
    public float FontScale = 1f;

    public TextRenderComponent(GameObject gameObject): base(gameObject)
    {
        if (fontMaterial == null)
        {
            fontMaterial = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Unlit_2D_Font, true);
            fontMaterial.SetUniform("AtlasScale", new Vector2(10, 10));
        }

        FontBuffer = GraphicsCache.GetInstancedUIBuffer(false);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{ this.GetType().Name }");

        return sb.ToString();
    }
    
    public override Component Clone(GameObject gameObject)
    {
        TextRenderComponent clone = new TextRenderComponent(gameObject);

        return clone;
    }

    public override void Render()
    {  
        if (!FontBuffer.DrawnThisFrame)
        {
            float charWidth = 0.05f * FontScale;
            float charHeight = 0.1f * FontScale;
            float lineOffsetX = 0f;
            float lineOffsetY = 0;

            float[] flatpack = new float[Text.Length *
                (
                VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
                VertexAttributeHelper.GetTypeByteSize(typeof(Vector2))
                )];

            int packIndex = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                char c = Text[i];

                if (c == '\n' || lineOffsetX + charWidth * GetCharWidth(c) > 10f)
                {
                    lineOffsetX = 0;
                    lineOffsetY -= charHeight * 2f;
                    if (c == '\n') 
                        continue;
                }

                if (TryGetCharacterAtlasOffset(c, out Vector2 atlasOffset))
                {
                    lineOffsetX += charWidth * GetCharWidth(c);
                    Vector2 charPosition = StartPosition + new Vector2(lineOffsetX, lineOffsetY);

                    Matrix4 modelMatrix = 
                        Matrix4.CreateScale(charWidth, charHeight, 1.0f) *
                        Matrix4.CreateTranslation(charPosition.X, charPosition.Y, 0.0f);

                    //PACK
                    MatrixHelper.Matrix4ToArray(modelMatrix, out float[] values);
                    for(int j = 0; j < values.Length; j++)
                        flatpack[packIndex++] = values[j];

                    flatpack[packIndex++] = atlasOffset.X;
                    flatpack[packIndex++] = atlasOffset.Y;
                }
            }

            FontBuffer.SetDynamicVBOData(flatpack, Text.Length);

            fontMaterial?.Bind();
            
            FontBuffer.DrawInstancedWithoutIndices(6, Text.Length);

            fontMaterial?.Unbind();

            FontBuffer.DrawnThisFrame = true;
        }
    }

    private bool TryGetCharacterAtlasOffset(char c, out Vector2 atlasOffset)
    {
        int charsPerRow = 10;
        int charSize = 100;
        int atlasWidth = 1000;
        int atlasHeight = 1000;

        int index = Charset.IndexOf(c);
        if (index < 0)
        {
            atlasOffset = Vector2.Zero;
            return false;
        }

        int x = index % charsPerRow;
        int y = index / charsPerRow;

        atlasOffset = new Vector2(
            (float)x * charSize / atlasWidth,
            1.0f - ((float)(y + 1) * charSize / atlasHeight) // Flip Y-axis
        );

        return true;
    }

    float GetCharWidth(char c)
    {
        return charWidths.ContainsKey(c) ? charWidths[c] : 1.5f;
    }

    private readonly Dictionary<char, float> charWidths = new Dictionary<char, float>
    {
        { 'i', 1.1f }, { 'j', 1.1f }, { 'l', 1.1f }, { '!', 1.1f }, { '|', 1.1f }, { '\'', 1.1f }, { '`', 1.1f }, 
        { ':', 1.1f }, { ';', 1.1f },

        { 'm', 1.9f }, { 'w', 1.9f }, { 'M', 1.9f }, { 'W', 1.9f }, { '@', 1.9f },

        { ' ', 1.5f }, { '.', 1.3f }, { ',', 1.3f }, { '-', 1.3f }, { '_', 1.9f }, { '=', 1.9f }, { '(', 1.3f }, 
        { ')', 1.3f }, { '[', 1.3f }, { ']', 1.3f }, { '{', 1.3f }, { '}', 1.3f }, { '<', 1.3f }, { '>', 1.3f },
    };
}
