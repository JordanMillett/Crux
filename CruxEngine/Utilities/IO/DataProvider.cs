using System.Text.Json;
using CruxEngine.Graphics;
using System.Reflection;
using StbImageSharp;
using OpenTK.Windowing.Common.Input;
using CruxEngine.Graphics.Shaders;

namespace CruxEngine.Utilities.IO;

public static class DataProvider
{
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters =
        {
            new QuaternionJsonConverter(),
            new Vector3JsonConverter(),
            new Vector2JsonConverter(),
            new Color4JsonConverter()
        }
    };
    
    public static string RootDirectory = "Game/Assets";

    /*
    public static void ListAllEmbeddedResources()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Logger.Log(assembly.GetManifestResourceNames().Length, LogSource.System);
        Logger.Log("Assembly Location: " + assembly.Location, LogSource.System);

        // Get all embedded resource names
        string[] resourceNames = assembly.GetManifestResourceNames();

        // Output all resource names
        Logger.Log("Embedded resources in the assembly:", LogSource.System);
        foreach (var resource in resourceNames)
        {
            Logger.Log(resource, LogSource.System);
        }
    }
    */

    public static Stream GetExternalStream(string path)
    {
        if(string.IsNullOrEmpty(path))
            return null!;

        Stream stream = File.OpenRead(path)!;
        if (stream == null)
        {
            Logger.LogWarning($"File '{path}' not found.");
            return null!;
        }
        return stream;
    }

    public static Stream GetEmbeddedStream(string path, bool silenceWarning = false)
    {
        if(string.IsNullOrEmpty(path))
            return null!;
        
        path = path.Replace("/", "\\");
        Assembly assembly = Assembly.GetExecutingAssembly();

        Stream stream = assembly.GetManifestResourceStream(path)!;
        if (stream == null)
        {
            if(!silenceWarning)
                Logger.LogWarning($"Embedded File '{path}' not found.");
            return null!;
        }
        return stream;
    }

    public static bool EmbeddedFileExists(string path)
    {
        return GetEmbeddedStream(path, true) != null;
    }

    public static string ReadEmbeddedFileInFull(string path)
    {
        using (var stream = GetEmbeddedStream(path))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static string ReadExternalFileInFull(string path)
    {
        using (var stream = GetExternalStream(path))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static WindowIcon LoadIcon()
    {
        string path = "CruxEngine/Assets/logo.png";
        
        using (Stream stream = GetEmbeddedStream(path)) 
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            byte[] pixelData = image.Data;
            Image iconImage = new Image(image.Width, image.Height, pixelData);
            
            return new WindowIcon(iconImage);
        }
    }

    //Move everything down

    public static int IterateBuildNumber()
    {
        string path = "CruxEngine/Assets/history.json";
        Dictionary<string, int> history = [];
        int buildNumber = 1;

        if(EmbeddedFileExists(path))
        {
            string data = ReadEmbeddedFileInFull(path);
            history = JsonSerializer.Deserialize<Dictionary<string, int>>(data) ?? [];

            if (history.TryGetValue(GameEngine.Version.ToString(), out buildNumber))
            {
                if(GameEngine.InDebugMode())
                    buildNumber++;
                history[GameEngine.Version.ToString()] = buildNumber;
            }
            else
            {
                history.Add(GameEngine.Version.ToString(), buildNumber);
            }

        }else
        {
            history.Add(GameEngine.Version.ToString(), buildNumber);
        }

        if(GameEngine.InDebugMode())
        {
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(history, JsonOptions));
            }catch
            {
                Logger.LogWarning("Failed to iterate build number.");
            }
        }
            

        return buildNumber;
    }
}
