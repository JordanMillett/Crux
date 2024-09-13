namespace Crux.Utilities;

public struct VersionData
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public VersionData(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}