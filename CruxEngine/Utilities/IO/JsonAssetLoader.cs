using System.Text.Json;
using CruxEngine.Graphics;
using System.Reflection;
using StbImageSharp;
using OpenTK.Windowing.Common.Input;
using CruxEngine.Graphics.Shaders;
using System.Text.Json.Serialization;

namespace CruxEngine.Utilities.IO;

public abstract class JsonPreset
{
    [JsonPropertyOrder(-100)]
    public string JsonPresetType { get; init; }

    protected JsonPreset()
    {
        JsonPresetType = GetType().Name;
    }
}

public static class JsonPresetLoader
{
    public const string TemplateSkyboxJsonPresetPath = "CruxEngine/Assets/Templates/Skybox.json";
    public const string TemplateSceneLightingJsonPresetPath = "CruxEngine/Assets/Templates/SceneLighting.json";

    static JsonPresetLoader()
    {
        if(GameEngine.InDebugMode())
        {
            try
            {
                File.WriteAllText(TemplateSkyboxJsonPresetPath, JsonSerializer.Serialize(new SkyboxJsonPreset(), DataProvider.JsonOptions));
                File.WriteAllText(TemplateSceneLightingJsonPresetPath, JsonSerializer.Serialize(new SceneLightingJsonPreset(), DataProvider.JsonOptions));
            }catch
            {
                Logger.LogWarning("Failed to write template files.");
            }
        }
    }

    public static SkyboxShader LoadSkybox(string embeddedPath)
    {
        SkyboxShader created = (SkyboxShader) Presets.LoadPresetShader(Presets.ShaderPresets.Unlit_2D_Skybox, false);

        SkyboxJsonPreset loaded = null!;
        if(DataProvider.EmbeddedFileExists(embeddedPath))
        {
            string data = DataProvider.ReadEmbeddedFileInFull(embeddedPath);
            loaded = JsonSerializer.Deserialize<SkyboxJsonPreset>(data, DataProvider.JsonOptions)!;
        }

        if(loaded != null)
        {
            created.TopColor = loaded.TopColor;
            created.MiddleColor = loaded.MiddleColor;
            created.BottomColor = loaded.BottomColor;
        }

        return created;
    }
    
    public static SceneLighting LoadSceneLighting(string embeddedPath)
    {
        SceneLighting created = new SceneLighting();

        SceneLightingJsonPreset loaded = null!;
        if(DataProvider.EmbeddedFileExists(embeddedPath))
        {
            string data = DataProvider.ReadEmbeddedFileInFull(embeddedPath);
            loaded = JsonSerializer.Deserialize<SceneLightingJsonPreset>(data, DataProvider.JsonOptions)!;
        }

        if(loaded != null)
        {
            created.AmbientColor = loaded.AmbientColor;
            created.FogColor = loaded.FogColor;
            created.SunColor = loaded.SunColor;
            created.SunIntensity = loaded.SunIntensity;
            created.AlphaFadeStart = loaded.AlphaFadeStart;
            created.AlphaFadeEnd = loaded.AlphaFadeEnd;
            created.FogStart = loaded.FogStart;
            created.FogEnd = loaded.FogEnd;
        }

        return created;
    }
}
