using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using CruxEngine.Utilities.IO;
using CruxEngine.Graphics;

namespace CruxEngine.Core;

public static class Profiler
{
    public class SnapShot
    {
        public long PrivateMemory;
        public long WorkingSetMemory;
        public long SharedMemory;
        public long ManagedMemory;
        public long UnmanagedMemory;

        public int GenerationOne;
        public int GenerationTwo;
        public int GenerationThree;
        public int Threads;
        public int Handles;

        public SnapShot()
        {
            PrivateMemory = Process.GetCurrentProcess().PrivateMemorySize64; 
            WorkingSetMemory = Process.GetCurrentProcess().WorkingSet64;
            SharedMemory =  WorkingSetMemory - PrivateMemory;
            ManagedMemory = GC.GetTotalMemory(false); 
            UnmanagedMemory = PrivateMemory - ManagedMemory;

            GenerationOne = GC.CollectionCount(0);
            GenerationTwo = GC.CollectionCount(1);
            GenerationThree = GC.CollectionCount(2);
            Threads = Process.GetCurrentProcess().Threads.Count;
            Handles = Process.GetCurrentProcess().HandleCount;
        }
    }

    private static Timer? ReportTimer;

    private static SnapShot Last = null!;

    public static void Start()
    {  
        ReportTimer = new(_ => Report(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        Logger.Log($"Profiler Started.", LogSource.System);
    }

    public static void Report()
    {
        try
        {   
            if(Last == null)
            {   
                Last = new SnapShot();
                return;
            }            

            SnapShot Taken = new SnapShot();

            string spacing = "";

            spacing = "{0,-20}{1,-20}{2,-20}{3}";
            Logger.Log($"==== Profiler Report ====", LogSource.System);
            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, "Memory", "Amount", "Difference", "Info"), LogSource.System);
            Logger.Log(string.Format(spacing, "Working Set", $"{ToMB(Taken.WorkingSetMemory)}", $"{ToMBDiff(Taken.WorkingSetMemory - Last.WorkingSetMemory)}", "Physical Process Utilized Memory"), LogSource.System);
            Logger.Log(string.Format(spacing, "-Shared", $"{ToMB(Taken.SharedMemory)}", $"{ToMBDiff(Taken.SharedMemory - Last.SharedMemory)}", "Shared Memory"), LogSource.System);
            Logger.Log(string.Format(spacing, "-Private", $"{ToMB(Taken.PrivateMemory)}", $"{ToMBDiff(Taken.PrivateMemory - Last.PrivateMemory)}", "Virtual Process Reserved Memory"), LogSource.System);
            Logger.Log(string.Format(spacing, "--Managed", $"{ToMB(Taken.ManagedMemory)}", $"{ToMBDiff(Taken.ManagedMemory - Last.ManagedMemory)}", ".NET Managed Memory"), LogSource.System);
            Logger.Log(string.Format(spacing, "--Unmanaged", $"{ToMB(Taken.UnmanagedMemory)}", $"{ToMBDiff(Taken.UnmanagedMemory - Last.UnmanagedMemory)}", "Unmanaged Memory"), LogSource.System);
            
            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, "Feature", "Count", "Difference", "Info"), LogSource.System);
            Logger.Log(string.Format(spacing, "Gen 1 GC", $"{Taken.GenerationOne}x", $"{ToCountDiff(Taken.GenerationOne - Last.GenerationOne)}", "Short-Lived Garbage Collection"), LogSource.System);
            Logger.Log(string.Format(spacing, "Gen 2 GC", $"{Taken.GenerationTwo}x", $"{ToCountDiff(Taken.GenerationTwo - Last.GenerationTwo)}", "Medium-Lived Garbage Collection"), LogSource.System);
            Logger.Log(string.Format(spacing, "Gen 3 GC", $"{Taken.GenerationThree}x", $"{ToCountDiff(Taken.GenerationThree - Last.GenerationThree)}", "Long-Lived Garbage Collection"), LogSource.System);
            Logger.Log(string.Format(spacing, "Threads", $"{Taken.Threads}x", $"{ToCountDiff(Taken.Threads - Last.Threads)}", "Running Threads"), LogSource.System);
            Logger.Log(string.Format(spacing, "Handles", $"{Taken.Handles}x", $"{ToCountDiff(Taken.Handles - Last.Handles)}", "OS Handles"), LogSource.System);
            Logger.Log("", LogSource.System);
            Logger.Log("--------------------------", LogSource.System);

            Logger.Log(GraphicsCache.GetFullInfo(), LogSource.System);

            Last = Taken; 
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    static string ToMB(long bytes)
    {
        return $"{bytes / 1024 / 1024} MB";
    }

    static string ToMBDiff(long bytes)
    {
        return bytes >= 0 ? $"+{ToMB(bytes)}" : $"-{ToMB(-bytes)}";
    }

    static string ToCountDiff(int amount)
    {
        return amount >= 0 ? $"+{amount}x" : $"-{-amount}x";
    }
}