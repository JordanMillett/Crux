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

    public static Stream GetExternalStream(string relativePath)
    {
        if(string.IsNullOrEmpty(relativePath))
            return null!;

        Stream stream = File.OpenRead(relativePath)!;
        if (stream == null)
        {
            Logger.LogWarning($"File '{relativePath}' not found.");
            return null!;
        }
        return stream;
    }

    public static Stream GetEmbeddedStream(string embeddedPath)
    {
        if(string.IsNullOrEmpty(embeddedPath))
            return null!;
        
        embeddedPath = embeddedPath.Replace("/", "\\");
        Assembly assembly = Assembly.GetExecutingAssembly();

        Stream stream = assembly.GetManifestResourceStream(embeddedPath)!;
        if (stream == null)
        {
            Logger.LogWarning($"No exact embedded file match found for '{embeddedPath}'");
            return null!;
        }
        return stream;
    }

    public static bool EmbeddedFileExists(string embeddedPath)
    {
        return GetEmbeddedStream(embeddedPath) != null;
    }

    public static string NearbyEmbeddedFileExists(string embeddedPath)
    {
        if(string.IsNullOrEmpty(embeddedPath))
            return null!;

        if(Path.HasExtension(embeddedPath))
            return embeddedPath;
        
        embeddedPath = embeddedPath.Replace("/", "\\");
        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();

        List<string> matches = resourceNames
            .Where(r => Path.ChangeExtension(r, null).ToLower() == Path.ChangeExtension(embeddedPath, null).ToLower())
            .ToList();

        if (matches.Count == 0)
        {
            Logger.LogWarning($"No exact or nearby embedded file matches found for '{embeddedPath}'");
            return null!;
        }

        if (matches.Count > 1)
        {
            Logger.LogWarning($"Multiple exact or nearby embedded file matches found for '{embeddedPath}'");
        }

        return matches[0];
    }

    public static List<string> GetNearbyFileNames(string embeddedPath)
    {
        if(string.IsNullOrEmpty(embeddedPath))
            return new List<string>();

        if(Path.HasExtension(embeddedPath))
        {
            Logger.LogWarning("Do not provide a file extension when retrieving nearby file names.");
            return new List<string>();
        }
        
        embeddedPath = embeddedPath.Replace("/", "\\");
        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();

        List<string> matches = resourceNames
            .Where(r => r.Contains(embeddedPath, StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            Logger.LogWarning($"No nearby file names found for search term '{embeddedPath}'");
            return new List<string>();
        }

        return matches;
    }

    public static string ReadEmbeddedFileInFull(string embeddedPath)
    {
        using (var stream = GetEmbeddedStream(embeddedPath))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static string ReadExternalFileInFull(string relativePath)
    {
        using (var stream = GetExternalStream(relativePath))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public static WindowIcon LoadIcon()
    {
        string embeddedPath = "CruxEngine/Assets/logo.png";
        
        using (Stream stream = GetEmbeddedStream(embeddedPath)) 
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
        string embeddedPath = "CruxEngine/Assets/history.json";
        Dictionary<string, int> history = [];
        int buildNumber = 1;

        if(EmbeddedFileExists(embeddedPath))
        {
            string data = ReadEmbeddedFileInFull(embeddedPath);
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
                File.WriteAllText(embeddedPath, JsonSerializer.Serialize(history, JsonOptions));
            }catch
            {
                Logger.LogWarning("Failed to iterate build number.");
            }
        }
            

        return buildNumber;
    }
}
