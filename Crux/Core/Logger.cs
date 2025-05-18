using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace Crux.Core;

public enum LogSource
{
    User,
    System,
    Warning,
    Error,
    Context
}

public static class Logger
{
    public static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "logs.txt");
    private static readonly ConcurrentQueue<string> PendingLogs = new();
    public static readonly DateTime StartTime;

    private static readonly Timer LogWriteTimer = new(_ => WritePendingLogsToFile(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

    static Logger()
    {
        StartTime = DateTime.UtcNow;

        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;

        File.WriteAllText(LogPath, "");
        
        Log($"Crux Engine Logs @ UTC {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}", LogSource.System);
    }

    public static void WritePendingLogsToFile()
    {
        if (PendingLogs.IsEmpty) 
            return;

        var chunk = new StringBuilder();
        while (PendingLogs.TryDequeue(out var log))
            chunk.AppendLine(log);

        try
        {
            File.AppendAllText(LogPath, chunk.ToString());
        }
        catch 
        {
            LogWarning($"Unable to write logs to file: {LogPath}");
        }
    }

    public static void Log<T>(T obj, LogSource source = LogSource.User)
    {
        if(obj == null || string.IsNullOrWhiteSpace(obj.GetType().Name))
        {
            LogWarning($"Type '{typeof(T).Name}' was unable to log due to being null.");
        }else
        {
            Log(obj.ToString()!, source);
        }
    }
    
    public static void Log(string message, LogSource source = LogSource.User)
    {
        Console.ForegroundColor = source switch
        {
            LogSource.User => ConsoleColor.White,
            LogSource.System => ConsoleColor.Green,
            LogSource.Warning => ConsoleColor.Yellow,
            LogSource.Error => ConsoleColor.Red,
            LogSource.Context => ConsoleColor.DarkGray,
            _ => ConsoleColor.Red
        };

        string prefix = source switch
        {
            LogSource.User => "Txt",
            LogSource.System => "Sys",
            LogSource.Warning => "Wrn",
            LogSource.Error => "Err",
            LogSource.Context => "Inf",
            _ => "???"
        };

        string time = (DateTime.UtcNow - StartTime).ToString(@"hh\:mm\:ss\:fff");
        string line = $"[{time}]\t({prefix})\t{message}";
        Console.WriteLine(line);
        PendingLogs.Enqueue(line);

        Console.ForegroundColor = ConsoleColor.White;
    }

    public static void LogWarning(string message,
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0,
    [CallerMemberName] string function = "")
    {
        string path = TrimPath(file);
        Log($"{message}", LogSource.Warning);
        Log($"^ {path}({line},1) -> {function}()", LogSource.Context);
    }

    public static void LogError(Exception ex,
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0,
    [CallerMemberName] string function = "")
    {
        string path = TrimPath(file);
        Log(ex.Message, LogSource.Error);
        Log($"^ {path}({line},1) -> {function}()", LogSource.Context);
    }

    private const string RootMarker = @"\Crux\";
    public static string TrimPath(string absolute)
    {
        int idx = absolute.IndexOf(RootMarker, StringComparison.OrdinalIgnoreCase);
        string created = idx >= 0 ? absolute.Substring(idx + 1) : Path.GetFileName(absolute);
        created = created.Substring(5, created.Length - 5);
        return created;
    }
}
