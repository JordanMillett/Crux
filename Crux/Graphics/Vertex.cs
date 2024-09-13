namespace Crux.Graphics;

public struct Vertex
{
    public Vector3 Position { get; }
    public Vector3 Normal { get; }
    public Vector2 UV { get; }

    public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Normal = normal;
        UV = uv;
    }
    
    public Vertex Clone()
    {
        return new Vertex(Position, Normal, UV);
    }
}
