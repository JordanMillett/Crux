using Crux.Graphics;
using Crux.Utilities.IO;
using Crux.Utilities.Helpers;
using Crux.Components;
using AngleSharp.Text;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Crux.CUI;

public struct CUILetterDraw
{
    public char Character;
    public CUIFontCharacter Font;
    public float ResolvedFontMultiplier;
    public float ResolvedFontSize;
    public Vector2 AbsolutePosition;
    public Color4 Hue;
}

public class CUIFont
{
    public readonly Dictionary<char, CUIFontCharacter> Characters = [];

    public float TextureWidth { get; init; }
    public float TextureHeight { get; init; }
    public float FontSize { get; init; }

    public CUIFont(string path)
    {
        string json = AssetHandler.ReadAssetInFull(path);
        JsonNode root = JsonNode.Parse(json)!;
        TextureWidth = (float)root["width"]!;
        TextureHeight = (float)root["height"]!;
        FontSize = (float)root["size"]!;

        JsonObject characters = root["characters"]!.AsObject();
        foreach (var pair in characters)
        {
            char c = pair.Key[0];
            JsonObject data = pair.Value!.AsObject();

            float x = (float)data["x"]!;
            float y = (float)data["y"]!;
            float width = (float)data["width"]!;
            float height = (float)data["height"]!;
            float originX = (float)data["originX"]!;
            float originY = (float)data["originY"]!;
            float advance = (float)data["advance"]!;

            CUIFontCharacter f = new CUIFontCharacter
            {
                DrawOffset = new Vector2(originX, originY),
                DrawSize = new Vector2(width, height),
                DrawAdvance = advance,
                UVOffset = new Vector2(x / TextureWidth, 1.0f - (y + height) / TextureHeight),
                UVScale = new Vector2(width / TextureWidth, height / TextureHeight),
            };

            Characters[c] = f;
        }
    }
}

public struct CUIFontCharacter
{
    public Vector2 DrawOffset;
    public Vector2 DrawSize;
    public float DrawAdvance;
    public Vector2 UVOffset;
    public Vector2 UVScale;
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

    static readonly List<CUILetterDraw> LettersToDraw = [];

    public string FontPath = "Crux/Assets/Fonts/Verdana";

    static CUIFont? loadedFont;

    public CUIText(CanvasComponent canvas): base(canvas)
    {
        if (ShaderSingleton == null)
        {
            ShaderSingleton = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Unlit_2D, true, $"{FontPath}.png");
            loadedFont = new CUIFont($"{FontPath}.json");

            ShaderSingleton.SetUniform("useSDF", 1f);
        }

        meshBuffer = GraphicsCache.GetInstancedQuadBuffer($"CUIText");
        InstanceID++;
    }

    public override void Measure() //NEED TO SUPPORT PADDING!!!!!
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
                    Logger.LogWarning($"CUICanvas bind point {key} not assigned.");

                i = end;
            }else
            {
                builder.Append(c);
                i++;
            }
        }

        RenderText = builder.ToString();

        //CALC
        //float availableWidth = Parent?.Bounds.Width.Resolved ?? GameEngine.Link.Resolution.X;
        float availableWidth = (Parent?.Bounds.LayoutMode == CUILayoutMode.Block)
            ? Parent.Bounds.Width.Resolved
            : GameEngine.Link.Resolution.X;
        float availableHeight = Parent?.Bounds.Height.Resolved ?? GameEngine.Link.Resolution.Y;
        FontSize.Resolve(CUIUnit.DefaultFontSizePixels); //BASE FONT SIZE

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

            float fontScale = FontSize.Resolved / loadedFont!.FontSize;
            float wordWidth = 0;
            foreach (char c in word)
            {
                if (c == '\n') break;
                float charWidth = FontSize.Resolved;
                wordWidth += loadedFont.Characters[c].DrawAdvance * fontScale;
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
                
                float charWidth = FontSize.Resolved;

                LettersToDraw.Add(new CUILetterDraw
                {
                    Character = c,
                    Font = loadedFont.Characters[c],
                    ResolvedFontMultiplier = fontScale,
                    ResolvedFontSize = FontSize.Resolved,
                    AbsolutePosition = new Vector2(Bounds.AbsolutePosition.X + cursorX, Bounds.AbsolutePosition.Y + cursorY),
                    Hue = FontColor
                });

                cursorX += loadedFont.Characters[c].DrawAdvance * fontScale;
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
            float[] flatpack = new float[LettersToDraw.Count *
            (
            VertexAttributeHelper.GetTypeByteSize(typeof(Matrix4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector4)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector2)) +
            VertexAttributeHelper.GetTypeByteSize(typeof(Vector2))
            )];

            int packIndex = 0;
            foreach (CUILetterDraw letter in LettersToDraw)
            {
                Matrix4 modelMatrix = Canvas.GetModelMatrix
                (
                    letter.Font.DrawSize.X * letter.ResolvedFontMultiplier,
                    letter.Font.DrawSize.Y * letter.ResolvedFontMultiplier,
                    letter.AbsolutePosition.X - letter.Font.DrawOffset.X * letter.ResolvedFontMultiplier,
                    letter.AbsolutePosition.Y - (letter.Font.DrawOffset.Y * letter.ResolvedFontMultiplier) + letter.ResolvedFontSize
                );

                // Convert modelMatrix to float array
                MatrixHelper.Matrix4ToArray(modelMatrix, out float[] values);

                // Copy matrix floats to flatpack
                for (int j = 0; j < values.Length; j++)
                    flatpack[packIndex++] = values[j];

                flatpack[packIndex++] = letter.Hue.R;
                flatpack[packIndex++] = letter.Hue.G;
                flatpack[packIndex++] = letter.Hue.B;
                flatpack[packIndex++] = letter.Hue.A;

                flatpack[packIndex++] = letter.Font.UVOffset.X;
                flatpack[packIndex++] = letter.Font.UVOffset.Y;

                flatpack[packIndex++] = letter.Font.UVScale.X;
                flatpack[packIndex++] = letter.Font.UVScale.Y;
            }

            meshBuffer.SetDynamicVBOData(flatpack, LettersToDraw.Count);

            ShaderSingleton?.Bind();

            meshBuffer.DrawInstancedWithoutIndices(Shapes.QuadVertices.Length, LettersToDraw.Count);

            ShaderSingleton?.Unbind();

            meshBuffer.DrawnThisFrame = true;

            LettersToDraw.Clear();
        }
    }
}