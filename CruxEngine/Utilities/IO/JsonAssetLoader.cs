using System.Text.Json;
using CruxEngine.Graphics;
using System.Reflection;
using StbImageSharp;
using OpenTK.Windowing.Common.Input;
using CruxEngine.Graphics.Shaders;
using System.Text.Json.Serialization;

namespace CruxEngine.Utilities.IO;

public abstract class JsonAsset
{
    [JsonPropertyOrder(-100)]
    public string JsonAssetType { get; init; }

    protected JsonAsset()
    {
        JsonAssetType = GetType().Name;
    }
}

public static class JsonAssetLoader
{
    static JsonAssetLoader()
    {
        if(GameEngine.InDebugMode())
        {
            try
            {
                File.WriteAllText("CruxEngine/Assets/Templates/Skybox.json", JsonSerializer.Serialize(new SkyboxDTO(), DataProvider.JsonOptions));
            }catch
            {
                Logger.LogWarning("Failed to write template files.");
            }
        }
    }

    public static SkyboxShader LoadSkybox(string embeddedPath)
    {
        SkyboxShader created = (SkyboxShader) Presets.LoadPresetShader(Presets.ShaderPresets.Unlit_2D_Skybox, false);

        SkyboxDTO loaded = null!;
        if(DataProvider.EmbeddedFileExists(embeddedPath))
        {
            string data = DataProvider.ReadEmbeddedFileInFull(embeddedPath);
            loaded = JsonSerializer.Deserialize<SkyboxDTO>(data, DataProvider.JsonOptions)!;
        }

        created.TopColor = loaded.TopColor;
        created.MiddleColor = loaded.MiddleColor;
        created.BottomColor = loaded.BottomColor;

        return created;
    }
}
