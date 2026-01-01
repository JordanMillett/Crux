using System.Text.Json;
using CruxEngine.Graphics;
using System.Reflection;
using StbImageSharp;
using OpenTK.Windowing.Common.Input;
using CruxEngine.Graphics.Shaders;

namespace CruxEngine.Utilities.IO;

public abstract class JsonAsset
{
    public Type JsonAssetType = typeof(JsonAsset);
}

public static class JsonAssetLoader
{
    public static SkyboxShader LoadSkybox(string embeddedPath)
    {
        SkyboxShader loaded = (SkyboxShader) Presets.LoadPresetShader(Presets.ShaderPresets.Unlit_2D_Skybox, false);

        

        return loaded;
    }
}
