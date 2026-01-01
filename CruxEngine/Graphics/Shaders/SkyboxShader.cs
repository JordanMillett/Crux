using OpenTK.Graphics.OpenGL4;
using CruxEngine.Utilities.Helpers;

namespace CruxEngine.Graphics.Shaders;

public class SkyboxJson
{
    public Color4 test;
}

public class SkyboxShader : Shader
{
    private Color4 _topColor = ColorHelper.Missing;
    public Color4 TopColor 
    { 
        get { return _topColor; } 
        set 
        { 
            _topColor = value;
            SetUniform("topColor", _topColor);
        } 
    }

    private Color4 _middleColor = ColorHelper.Missing;
    public Color4 MiddleColor 
    { 
        get { return _middleColor; } 
        set 
        { 
            _middleColor = value;
            SetUniform("middleColor", _middleColor);
        } 
    }

    private Color4 _bottomColor = ColorHelper.Missing;
    public Color4 BottomColor 
    { 
        get { return _bottomColor; } 
        set 
        { 
            _bottomColor = value;
            SetUniform("bottomColor", _bottomColor);
        } 
    }

    public SkyboxShader(string vertexShaderPath, string fragmentShaderPath, string colorTexturePath, bool useInstancing) : base(vertexShaderPath, fragmentShaderPath, colorTexturePath, useInstancing)
    {

    }
}
