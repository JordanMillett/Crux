using OpenTK.Graphics.OpenGL4;

namespace Crux.Graphics;

public class VertexAttribute
{
    public int LayoutLocation { get; }
    public string LayoutName { get; }
    public int TypeSize { get; }
    public float[] Data { get; }

    public VertexAttribute(int layoutLocation, string layoutName, int typeSize, float[] data)
    {
        LayoutLocation = layoutLocation;
        LayoutName = layoutName;
        TypeSize = typeSize;
        Data = data;
    }
}