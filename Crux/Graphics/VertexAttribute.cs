using OpenTK.Graphics.OpenGL4;

namespace Crux.Graphics;

public class VertexAttribute
{
    public string LayoutName { get; }
    public int TypeSize { get; }
    public float[] Data { get; }

    public VertexAttribute(string layoutName, int typeSize, float[] data)
    {
        LayoutName = layoutName;
        TypeSize = typeSize;
        Data = data;
    }
}