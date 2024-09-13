namespace Crux.Core;

public enum LogSource
{
    User,
    System,
    Warning,
    Error
}

public static class Logger
{
    public static readonly DateTime StartTime;

    static Logger()
    {
        StartTime = DateTime.UtcNow;

        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
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
            _ => ConsoleColor.Red
        };

        string prefix = source switch
        {
            LogSource.User => "Usr",
            LogSource.System => "Sys",
            LogSource.Warning => "Wrn",
            LogSource.Error => "Err",
            _ => "???"
        };

        string time = (DateTime.UtcNow - StartTime).ToString(@"hh\:mm\:ss\:fff");
        Console.WriteLine($"[{time}]\t({prefix})\t{message}");

        Console.ForegroundColor = ConsoleColor.White;
    }

    public static void LogWarning(string message)
    {
        Log(message, LogSource.Warning);
    }

    public static void LogError(Exception ex)
    {
        Log(ex.Message, LogSource.Error);
    }
}
