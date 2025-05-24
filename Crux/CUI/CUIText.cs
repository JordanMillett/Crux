using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Components;

namespace Crux.CUI;

public class CUIText : CUINode
{
    private static Shader? ShaderSingleton;
    private readonly MeshBuffer meshBuffer;
    private readonly string Charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public string Text { get; set; } = "";
    public static int InstanceID = 0;

    public float VirtualFontSize = 16f;

    public CUIText(CanvasComponent canvas): base(canvas)
    {
        if (ShaderSingleton == null)
        {
            ShaderSingleton = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Unlit_2D_Font, true);
            ShaderSingleton.SetUniform("atlasScale", new Vector2(10, 10));
        }

        meshBuffer = GraphicsCache.GetInstancedFontBuffer($"CUIText_{InstanceID}");
        InstanceID++;
    }

    public override void Measure()
    {
        float totalWidth = 0;
        float fontHeight = VirtualFontSize;

        foreach (char c in Text)
        {
            float charWidthFactor = GetCharWidth(c);
            float charWidth = fontHeight * charWidthFactor;

            totalWidth += charWidth;
        }

        Bounds.Width = totalWidth;
        Bounds.Height = fontHeight;
    }

    public override void Render()
    {
        if (!meshBuffer.DrawnThisFrame)
        {
            float charWidth = 0.05f * VirtualFontSize;
            float charHeight = 0.1f * VirtualFontSize;
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
                    Vector2 charPosition = Bounds.AbsolutePosition + new Vector2(lineOffsetX, lineOffsetY);

                    CUIBounds charBounds = new CUIBounds
                    {
                        Width = charWidth,
                        Height = charHeight,
                        AbsolutePosition = charPosition,
                        RelativePosition = Vector2.Zero,
                    };

                        Matrix4 modelMatrix = Canvas.GetLetterModelMatrix(charBounds);

                    //PACK
                    MatrixHelper.Matrix4ToArray(modelMatrix, out float[] values);
                    for(int j = 0; j < values.Length; j++)
                        flatpack[packIndex++] = values[j];

                    flatpack[packIndex++] = atlasOffset.X;
                    flatpack[packIndex++] = atlasOffset.Y;
                }
            }

            meshBuffer.SetDynamicVBOData(flatpack, Text.Length);

            ShaderSingleton?.Bind();
            
            meshBuffer.DrawInstancedWithoutIndices(Shapes.QuadVertices.Length, Text.Length);

            ShaderSingleton?.Unbind();

            meshBuffer.DrawnThisFrame = true;
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