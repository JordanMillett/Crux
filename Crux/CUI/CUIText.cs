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

    public float VirtualFontSize = 32f;
    public Color4 FontColor = Color4.White;

    public CUIText(CanvasComponent canvas): base(canvas)
    {
        if (ShaderSingleton == null)
        {
            ShaderSingleton = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Unlit_2D_Font, true);
            ShaderSingleton.SetUniform("atlasScale", new Vector2(10, 10));
        }

        meshBuffer = GraphicsCache.GetInstancedQuadBuffer($"CUIText_{InstanceID}");
        InstanceID++;
    }

    public override void Measure()
    {
        float totalWidth = 0;

        foreach (char c in Text)
        {
            float charWidth = VirtualFontSize * GetCharWidth(c) / 2f;
            totalWidth += charWidth;
        }

        Bounds.Width = totalWidth;
        Bounds.Height = VirtualFontSize;
    }

    public override void Render()
    {
        if (!meshBuffer.DrawnThisFrame)
        {
            float cursorX = -VirtualFontSize/6f;
            float cursorY = 0f;

            float[] flatpack = new float[Text.Length *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector2))
            )];

            int packIndex = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                char c = Text[i];

                if (c == '\n')
                {
                    cursorX = 0f;
                    cursorY -= VirtualFontSize; 
                    continue;
                }

                // Get character width scaled by VirtualFontSize
                float charWidth = VirtualFontSize * GetCharWidth(c) / 2f;

                // Calculate character bounds in virtual resolution space
                CUIBounds charBounds = new CUIBounds
                {
                    Width = VirtualFontSize,
                    Height = VirtualFontSize,
                    AbsolutePosition = Bounds.AbsolutePosition + new Vector2(cursorX, cursorY),
                    RelativePosition = Vector2.Zero,
                };

                // Get model matrix for this character
                Matrix4 modelMatrix = Canvas.GetModelMatrix(charBounds);

                // Convert modelMatrix to float array
                MatrixHelper.Matrix4ToArray(modelMatrix, out float[] values);

                // Copy matrix floats to flatpack
                for (int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];

                // Get atlas offset for this character
                if (!TryGetCharacterAtlasOffset(c, out Vector2 atlasOffset))
                {
                    atlasOffset = Vector2.Zero; // fallback if char not found
                }

                flatpack[packIndex++] = FontColor.R;
                flatpack[packIndex++] = FontColor.G;
                flatpack[packIndex++] = FontColor.B;
                flatpack[packIndex++] = FontColor.A;

                flatpack[packIndex++] = atlasOffset.X;
                flatpack[packIndex++] = atlasOffset.Y;

                cursorX += charWidth;
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