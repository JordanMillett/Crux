using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Components;
using AngleSharp.Text;
using System.Diagnostics;

namespace Crux.CUI;

public class CUIText : CUINode
{
    private static Shader? ShaderSingleton;
    private readonly MeshBuffer meshBuffer;
    private readonly string Charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public string TextData { get; set; } = "";
    public string RenderText { get; set; } = "";
    public static int InstanceID = 0;

    public CUIUnit FontSize = new CUIUnit(CUIUnitType.Pixel, 16);
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
        StringBuilder builder = new StringBuilder();
        int i = 0;
        while (i < TextData.Length)
        {
            char c = TextData[i];

            if(c == '@')
            {
                int start = i + 1;
                int end = start;
                while (end < TextData.Length && !char.IsWhiteSpace(TextData[end]))
                    end++;

                string key = TextData[start..end];
                if (Canvas.BindPoints.TryGetValue(key, out Func<string>? maker))
                    builder.Append(maker());
                else
                    Logger.LogWarning($"CUICanvas bind point {key} not found.");

                i = end;
            }else
            {
                builder.Append(c);
                i++;
            }
        }

        RenderText = builder.ToString();

        //CALC
        float availableWidth = Parent?.Bounds.Width.Resolved ?? GameEngine.Link.Resolution.X;
        float availableHeight = Parent?.Bounds.Height.Resolved ?? GameEngine.Link.Resolution.Y;
        FontSize.Resolve(16f); //BASE FONT SIZE

        float lineWidth = 0;
        float widestLine = 0;
        float totalHeight = FontSize.Resolved;
        foreach (char c in RenderText)
        {
            if (c == '\n')
            {
                totalHeight += FontSize.Resolved;
                widestLine = float.Max(widestLine, lineWidth);
                lineWidth = 0;
            }else
            {
                float charWidth = FontSize.Resolved * GetCharWidth(c) / 2f;
                lineWidth += charWidth;
            }
        }

        widestLine = float.Max(widestLine, lineWidth);

        //Resolve
        Bounds.Width.Resolve(availableWidth, widestLine);
        Bounds.Height.Resolve(availableHeight, totalHeight);
    }

    public override void Render()
    {
        if (!meshBuffer.DrawnThisFrame)
        {
            float cursorX = -FontSize.Resolved/7f;
            float cursorY = 0;

            float[] flatpack = new float[RenderText.Length *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector2))
            )];

            int packIndex = 0;
            for (int i = 0; i < RenderText.Length; i++)
            {
                char c = RenderText[i];

                if (c == '\n')
                {
                    cursorX = -FontSize.Resolved/7f;
                    cursorY += FontSize.Resolved; 
                    continue;
                }

                // Get character width scaled
                float charWidth = FontSize.Resolved * GetCharWidth(c) / 2f;

                // Get model matrix for this character
                Matrix4 modelMatrix = Canvas.GetModelMatrix
                (
                    FontSize.Resolved,
                    FontSize.Resolved,
                    Bounds.AbsolutePosition.X + cursorX,
                    Bounds.AbsolutePosition.Y + cursorY
                );

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

            meshBuffer.SetDynamicVBOData(flatpack, RenderText.Length);

            ShaderSingleton?.Bind();

            meshBuffer.DrawInstancedWithoutIndices(Shapes.QuadVertices.Length, RenderText.Length);

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