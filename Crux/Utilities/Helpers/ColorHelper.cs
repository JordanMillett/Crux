namespace Crux.Utilities.Helpers;

public static class ColorHelper
{
    public static float[] Flatten(Color4 input)
    {
        return new float[] { input.R, input.G, input.B, input.A };
    }

    public static Color4 HexToColor4(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = 0, g = 0, b = 0, a = 255;

        if (hex.Length == 6)
        {
            r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        }
        else if (hex.Length == 8)
        {
            r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }

        return new Color4(r, g, b, a);
    }
}
