using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using CruxEngine.Utilities.IO;

namespace CruxEngine.Core;

public static class Profiler
{
    private static Timer? ReportTimer;

    public static void Start()
    {  
        ReportTimer = new(_ => Report(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        Logger.Log($"Profiler Started.", LogSource.System);
    }

    public static void Report()
    {
        try
        {
            string spacing = "{0,-20}{1,-15}{2}";

            long managedMemory = GC.GetTotalMemory(false);
            long privateMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            long workingSetMemory = Process.GetCurrentProcess().WorkingSet64;

            Logger.Log($"==== Profiler Report ====", LogSource.System);
            Logger.Log(string.Format(spacing, "Memory", $"Usage", "Info"), LogSource.System);
            Logger.Log(string.Format(spacing, "Working Set", $"{workingSetMemory / 1024 / 1024} MB", "Physical RAM Allocation"), LogSource.System);
            Logger.Log(string.Format(spacing, "- Private", $"{privateMemory / 1024 / 1024} MB", "Exclusive Virtual Memory Allocation"), LogSource.System);
            Logger.Log(string.Format(spacing, "-- Managed", $"{managedMemory / 1024 / 1024} MB", ".NET Allocation"), LogSource.System);
            Logger.Log("--------------------------", LogSource.System);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }
}