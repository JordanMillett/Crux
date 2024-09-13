namespace Crux.Graphics;

public struct Plane
{
    public Vector3 Normal { get; set; }
    public float Distance { get; set; }

    public Plane(Vector3 normal, float distance)
    {
        Normal = normal;
        Distance = distance;
    }
    
    public Plane(Vector4 row)
    {
        Normal = new Vector3(row.X, row.Y, row.Z);
        Distance = row.W;
    }
    
    public void Normalize()
    {
        float length = Normal.Length;
        if (length > 0.0001f)
        {
            Normal /= length;
            Distance /= length;
        }
    }
    
    public Plane Clone()
    {
        return new Plane(Normal, Distance);
    }
}
