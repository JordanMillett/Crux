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
            long managedMemory = GC.GetTotalMemory(false);
            long exclusiveMemory = Process.GetCurrentProcess().PrivateMemorySize64;
            long processMemory = Process.GetCurrentProcess().WorkingSet64;

            Logger.Log($"[Profiler Report]", LogSource.System);
            Logger.Log($"Managed Memory (.NET Objects): {managedMemory / 1024 / 1024} MB", LogSource.System);
            Logger.Log($"Private Memory (Exclusive): {exclusiveMemory / 1024 / 1024} MB", LogSource.System);
            Logger.Log($"Process Memory: {processMemory / 1024 / 1024} MB", LogSource.System);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }
}