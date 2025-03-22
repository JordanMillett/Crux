using OpenTK.Graphics.OpenGL4;
using Crux.Graphics;
using Crux.Utilities.IO;

namespace Crux.Components;

public class TextRenderComponent : RenderComponent
{     
    public (int vao, int vbo) FontVAO;
    public static Dictionary<(int vao, int vbo), bool> Rendered = [];
    public static Dictionary<(int vao, int vbo), PerInstanceData> InstanceData = [];

    public struct PerInstanceData
    {
        public List<(Matrix4 model, Vector2 atlasOffset)> CharacterData;
        public int GPUBufferLength;
    }

    string Charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
    
    public static Shader fontMaterial = null!;
    public string Text = "";
    public Vector2 StartPosition = Vector2.Zero;
    public float FontScale = 1f;

    public TextRenderComponent(GameObject gameObject): base(gameObject)
    {
        if (fontMaterial == null)
        {
            fontMaterial = AssetHandler.LoadPresetShader(AssetHandler.ShaderPresets.Font);
            fontMaterial.SetUniform("AtlasScale", new Vector2(10, 10));
        }

        FontVAO = GraphicsCache.GetInstancedUIVAO();

        if (!InstanceData.ContainsKey(FontVAO))
        {
            PerInstanceData data = new PerInstanceData
            {
                CharacterData = new List<(Matrix4, Vector2)>(),
                GPUBufferLength = 0
            };

            InstanceData.Add(FontVAO, data);
            Rendered.Add(FontVAO, false);
        }
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
        int Matrix4SizeInBytes = 16 * sizeof(float);
        int Vector2SizeInBytes = 2 * sizeof(float);
        int InstanceSizeInBytes = Matrix4SizeInBytes + Vector2SizeInBytes;

        if (!InstanceData.ContainsKey(FontVAO)) 
            return;
        PerInstanceData data = InstanceData[FontVAO];

        if (!Rendered[FontVAO])
        {
            float charWidth = 0.05f * FontScale;
            float charHeight = 0.1f * FontScale;
            float lineOffsetX = 0f;
            float lineOffsetY = 0;

            data.CharacterData.Clear();

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

                    data.CharacterData.Add((modelMatrix, atlasOffset));
                }
            }

            if (data.CharacterData.Count > data.GPUBufferLength)
            {
                data.GPUBufferLength = Math.Max(64, data.GPUBufferLength * 2);
                InstanceData[FontVAO] = data;

                GL.BindBuffer(BufferTarget.ArrayBuffer, FontVAO.vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, data.GPUBufferLength * InstanceSizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, FontVAO.vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.CharacterData.Count * InstanceSizeInBytes, data.CharacterData.ToArray());
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            fontMaterial.Bind();
            GL.BindVertexArray(FontVAO.vao);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, data.CharacterData.Count);
            GraphicsCache.DrawCallsThisFrame++;
            GL.BindVertexArray(0);
            fontMaterial.Unbind();

            Rendered[FontVAO] = true;
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

    Dictionary<char, float> charWidths = new Dictionary<char, float>
    {
        { 'i', 1.1f }, { 'j', 1.1f }, { 'l', 1.1f }, { '!', 1.1f }, { '|', 1.1f }, { '\'', 1.1f }, { '`', 1.1f }, 
        { ':', 1.1f }, { ';', 1.1f },

        { 'm', 1.9f }, { 'w', 1.9f }, { 'M', 1.9f }, { 'W', 1.9f }, { '@', 1.9f },

        { ' ', 1.5f }, { '.', 1.3f }, { ',', 1.3f }, { '-', 1.3f }, { '_', 1.9f }, { '=', 1.9f }, { '(', 1.3f }, 
        { ')', 1.3f }, { '[', 1.3f }, { ']', 1.3f }, { '{', 1.3f }, { '}', 1.3f }, { '<', 1.3f }, { '>', 1.3f },
    };
}
