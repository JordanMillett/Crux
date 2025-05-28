using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Components;
using AngleSharp.Text;
using System.Diagnostics;

namespace Crux.CUI;

public struct CUILetter
{
    public char Character;
    public Vector2 RelativePosition;
    public float ResolvedWidth;
    public Vector2 AtlasOffset;
}

public class CUIText : CUINode
{
    private static Shader? ShaderSingleton;
    private readonly MeshBuffer meshBuffer;
    private readonly string Charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    public string TextData { get; set; } = "";
    public string RenderText { get; set; } = "";
    public static int InstanceID = 0;

    public CUIUnit FontSize = CUIUnit.DefaultFontSize;
    public Color4 FontColor = Color4.White;

    List<CUILetter> CalculatedLetters = [];

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
        FontSize.Resolve(CUIUnit.DefaultFontSizePixels); //BASE FONT SIZE

        CalculatedLetters.Clear();

        float cursorX = 0;
        float cursorY = 0;

        float lineHeight = FontSize.Resolved;
        float totalHeight = lineHeight;
        float widestLine = 0;

        int index = 0;
        while (index < RenderText.Length)
        {
            if (RenderText[index] == '\n')
            {
                cursorX = 0;
                cursorY += lineHeight;
                totalHeight += lineHeight;
                index++;
                continue;
            }

            int wordStart = index;
            while (index < RenderText.Length && RenderText[index] != ' ' && RenderText[index] != '\n')
                index++;
            while (index < RenderText.Length && RenderText[index] == ' ')
                index++;
            int wordEnd = index;

            string word = RenderText[wordStart..wordEnd];

            float wordWidth = 0;
            foreach (char c in word)
            {
                if (c == '\n') break;
                float charWidth = FontSize.Resolved * GetCharWidth(c) / 2f;
                wordWidth += charWidth;
            }

            if (cursorX + wordWidth > availableWidth && cursorX > 0)
            {
                cursorX = 0;
                cursorY += lineHeight;
                totalHeight += lineHeight;
            }

            foreach (char c in word)
            {
                if (c == '\n')
                    break;
                
                float charWidth = FontSize.Resolved * GetCharWidth(c) / 2f;
                if (!TryGetCharacterAtlasOffset(c, out Vector2 atlasOffset))
                    atlasOffset = Vector2.Zero;

                CalculatedLetters.Add(new CUILetter
                {
                    Character = c,
                    RelativePosition = new Vector2(cursorX, cursorY),
                    ResolvedWidth = charWidth,
                    AtlasOffset = atlasOffset
                });

                cursorX += charWidth;
            }

            widestLine = Math.Max(widestLine, cursorX);            
        }

        //Resolve
        Bounds.Width.Resolve(availableWidth, widestLine, FontSize.Resolved);
        Bounds.Height.Resolve(availableHeight, totalHeight, FontSize.Resolved);
    }

    public override void Render()
    {
        if (!meshBuffer.DrawnThisFrame)
        {
            float[] flatpack = new float[CalculatedLetters.Count *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector2))
            )];

            int packIndex = 0;
            foreach (CUILetter letter in CalculatedLetters)
            {
                Matrix4 modelMatrix = Canvas.GetModelMatrix
                (
                    FontSize.Resolved,
                    FontSize.Resolved,
                    Bounds.AbsolutePosition.X + letter.RelativePosition.X - FontSize.Resolved/7f,
                    Bounds.AbsolutePosition.Y + letter.RelativePosition.Y
                );

                // Convert modelMatrix to float array
                MatrixHelper.Matrix4ToArray(modelMatrix, out float[] values);

                // Copy matrix floats to flatpack
                for (int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];

                flatpack[packIndex++] = FontColor.R;
                flatpack[packIndex++] = FontColor.G;
                flatpack[packIndex++] = FontColor.B;
                flatpack[packIndex++] = FontColor.A;

                flatpack[packIndex++] = letter.AtlasOffset.X;
                flatpack[packIndex++] = letter.AtlasOffset.Y;
            }

            meshBuffer.SetDynamicVBOData(flatpack, CalculatedLetters.Count);

            ShaderSingleton?.Bind();

            meshBuffer.DrawInstancedWithoutIndices(Shapes.QuadVertices.Length, CalculatedLetters.Count);

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
        { 'i', 1.1f }, { 'j', 1.1f }, { 'l', 1.1f }, { 'r', 1.1f }, { '!', 2.0f }, { '|', 1.1f }, { '\'', 1.1f }, { '`', 1.1f }, 
        { ':', 1.1f }, { ';', 1.1f },

        { 'm', 1.9f }, { 'w', 1.5f }, { 'M', 1.9f }, { 'W', 1.9f }, { '@', 1.9f },

        { ' ', 1.25f }, { '.', 1.3f }, { ',', 1.3f }, { '-', 1.3f }, { '_', 1.9f }, { '=', 1.9f }, { '(', 1.3f }, 
        { ')', 1.3f }, { '[', 1.3f }, { ']', 1.3f }, { '{', 1.3f }, { '}', 1.3f }, { '<', 1.3f }, { '>', 1.3f },
    };
}