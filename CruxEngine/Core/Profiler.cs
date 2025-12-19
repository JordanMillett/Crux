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

        public int GenerationZero;
        public int GenerationOne;
        public int GenerationTwo;
        public int Threads;
        public int Handles;

        public float FPS;
        public int DrawCalls;
        public int Triangles;
        public int Lines;

        public int VAO;
        public int VAOUsers;
        public int Texture;
        public int TextureUsers;
        public int Vertex;
        public int VertexUsers;
        public int Fragment;
        public int FragmentUsers;
        public int Program;
        public int ProgramUsers;

        public SnapShot()
        {
            PrivateMemory = Process.GetCurrentProcess().PrivateMemorySize64; 
            WorkingSetMemory = Process.GetCurrentProcess().WorkingSet64;
            SharedMemory =  WorkingSetMemory - PrivateMemory;
            ManagedMemory = GC.GetTotalMemory(false); 
            UnmanagedMemory = PrivateMemory - ManagedMemory;

            GenerationZero = GC.CollectionCount(0);
            GenerationOne = GC.CollectionCount(1);
            GenerationTwo = GC.CollectionCount(2);
            Threads = Process.GetCurrentProcess().Threads.Count;
            Handles = Process.GetCurrentProcess().HandleCount;

            FPS = GraphicsCache.FramesPerSecond;
            DrawCalls = GraphicsCache.DrawCallsThisFrame;
            Triangles = GraphicsCache.TrianglesThisFrame;
            Lines = GraphicsCache.LinesThisFrame;

            VAO = GraphicsCache.VAOs.Count;
            VAOUsers = GraphicsCache.VAOs.Sum(entry => entry.Value.users);
            Texture = GraphicsCache.Textures.Count;
            TextureUsers = GraphicsCache.Textures.Sum(entry => entry.Value.users);
            Vertex = GraphicsCache.Vertex.Count;
            VertexUsers = GraphicsCache.Vertex.Sum(entry => entry.Value.users);
            Fragment = GraphicsCache.Fragment.Count;
            FragmentUsers = GraphicsCache.Fragment.Sum(entry => entry.Value.users);
            Program = GraphicsCache.Programs.Count;
            ProgramUsers = GraphicsCache.Programs.Sum(entry => entry.Value.users);
        }
    }

    private static Timer? ReportTimer;

    private static SnapShot Last = null!;

    public static void Start()
    {  
        ReportTimer = new(_ => Report(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
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
            Logger.Log(string.Format(spacing, "Feature", "Total", "Difference", "Info"), LogSource.System);
            Logger.Log(string.Format(spacing, "Gen 0 GC", $"{Taken.GenerationZero}x", $"{ToCountDiff(Taken.GenerationZero - Last.GenerationZero)}", "Short-Lived Garbage Collection"), LogSource.System);
            Logger.Log(string.Format(spacing, "Gen 1 GC", $"{Taken.GenerationOne}x", $"{ToCountDiff(Taken.GenerationOne - Last.GenerationOne)}", "Medium-Lived Garbage Collection"), LogSource.System);
            Logger.Log(string.Format(spacing, "Gen 2 GC", $"{Taken.GenerationTwo}x", $"{ToCountDiff(Taken.GenerationTwo - Last.GenerationTwo)}", "Long-Lived Garbage Collection"), LogSource.System);
            Logger.Log(string.Format(spacing, "Threads", $"{Taken.Threads}x", $"{ToCountDiff(Taken.Threads - Last.Threads)}", "Running Threads"), LogSource.System);
            Logger.Log(string.Format(spacing, "Handles", $"{Taken.Handles}x", $"{ToCountDiff(Taken.Handles - Last.Handles)}", "OS Handles"), LogSource.System);

            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, "Feature", "Value", "Difference", "Info"), LogSource.System);
            Logger.Log(string.Format(spacing, "FPS", $"{Taken.FPS:F2}", $"{ToFloatDiff(Taken.FPS - Last.FPS)}", "Frames Per Second"), LogSource.System);
            Logger.Log(string.Format(spacing, "Draw Calls", $"{Taken.DrawCalls}x", $"{ToCountDiff(Taken.DrawCalls - Last.DrawCalls)}", "GPU Draw Calls"), LogSource.System);
            Logger.Log(string.Format(spacing, "Triangles", $"{Taken.Triangles}x", $"{ToCountDiff(Taken.Triangles - Last.Triangles)}", "GPU Triangles Rendered"), LogSource.System);
            Logger.Log(string.Format(spacing, "Lines", $"{Taken.Lines}x", $"{ToCountDiff(Taken.Lines - Last.Lines)}", "GPU Lines Rendered"), LogSource.System);

            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, "Count", "Value", "Difference", "Info"), LogSource.System);
            Logger.Log(string.Format(spacing, "VAOs", $"{Taken.VAO}x", $"{ToCountDiff(Taken.VAO - Last.VAO)}", "Unique Vertex Array Objects"), LogSource.System);
            Logger.Log(string.Format(spacing, "Textures", $"{Taken.Texture}x", $"{ToCountDiff(Taken.Texture - Last.Texture)}", "Unique Textures"), LogSource.System);
            Logger.Log(string.Format(spacing, "Vertex", $"{Taken.Vertex}x", $"{ToCountDiff(Taken.Vertex - Last.Vertex)}", "Unique Vertex Shaders"), LogSource.System);
            Logger.Log(string.Format(spacing, "Fragment", $"{Taken.Fragment}x", $"{ToCountDiff(Taken.Fragment - Last.Fragment)}", "Unique Fragment Shaders"), LogSource.System);
            Logger.Log(string.Format(spacing, "Programs", $"{Taken.Program}x", $"{ToCountDiff(Taken.Program - Last.Program)}", "Unique Shader Programs"), LogSource.System);

            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, "Total", "Value", "Difference", "Info"), LogSource.System);
            Logger.Log(string.Format(spacing, "VAO Users", $"{Taken.VAOUsers}x", $"{ToCountDiff(Taken.VAOUsers - Last.VAOUsers)}", "Vertex Array Objects Users"), LogSource.System);
            Logger.Log(string.Format(spacing, "Texture Users", $"{Taken.TextureUsers}x", $"{ToCountDiff(Taken.TextureUsers - Last.TextureUsers)}", "Texture Users"), LogSource.System);
            Logger.Log(string.Format(spacing, "Vertex Users", $"{Taken.VertexUsers}x", $"{ToCountDiff(Taken.VertexUsers - Last.VertexUsers)}", "Vertex Shader Users"), LogSource.System);
            Logger.Log(string.Format(spacing, "Fragment Users", $"{Taken.FragmentUsers}x", $"{ToCountDiff(Taken.FragmentUsers - Last.FragmentUsers)}", "Fragment Shader Users"), LogSource.System);
            Logger.Log(string.Format(spacing, "Program Users", $"{Taken.ProgramUsers}x", $"{ToCountDiff(Taken.ProgramUsers - Last.ProgramUsers)}", "Shader Program Users"), LogSource.System);

            spacing = "{0,-25}{1}";
            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, $"{GraphicsCache.VAOs.Sum(entry => entry.Value.users)}x VAO Users", "Source"), LogSource.System);
            foreach (var entry in GraphicsCache.VAOs.OrderByDescending(e => e.Value.users))
                Logger.Log(string.Format(spacing, $"{entry.Value.users}x", $"{entry.Key}"), LogSource.System);
        
            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, $"{GraphicsCache.Textures.Sum(entry => entry.Value.users)}x Texture Users", "Source"), LogSource.System);
            foreach (var entry in GraphicsCache.Textures.OrderByDescending(e => e.Value.users))
                Logger.Log(string.Format(spacing, $"{entry.Value.users}x", $"{entry.Key}"), LogSource.System);

            spacing = "{0,-25}{1,-20}{2}";
            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, $"{GraphicsCache.Vertex.Sum(entry => entry.Value.users)}x Vertex Users", "Instanced", "Source"), LogSource.System);
            foreach (var entry in GraphicsCache.Vertex.OrderByDescending(e => e.Value.users))
                Logger.Log(string.Format(spacing, $"{entry.Value.users}x", $"{entry.Key.instanced}", $"{entry.Key.cacheKey}"), LogSource.System);

            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, $"{GraphicsCache.Vertex.Sum(entry => entry.Value.users)}x Fragment Users", "Instanced", "Source"), LogSource.System);
            foreach (var entry in GraphicsCache.Fragment.OrderByDescending(e => e.Value.users))
                Logger.Log(string.Format(spacing, $"{entry.Value.users}x", $"{entry.Key.instanced}", $"{entry.Key.cacheKey}"), LogSource.System);

            spacing = "{0,-25}{1,-20}{2,-20}{3,-20}{4}";
            Logger.Log("", LogSource.System);
            Logger.Log(string.Format(spacing, $"{GraphicsCache.Programs.Sum(entry => entry.Value.users)}x Program Users", "Instanced", "Vertex", "Instanced", "Fragment"), LogSource.System);
            foreach (var entry in GraphicsCache.Programs.OrderByDescending(e => e.Value.users))
            {
                var vertKey = GraphicsCache.Vertex.FirstOrDefault(f => f.Value.id == entry.Key.vertId).Key;
                var fragKey = GraphicsCache.Fragment.FirstOrDefault(f => f.Value.id == entry.Key.fragId).Key;

                Logger.Log(string.Format(spacing, $"{entry.Value.users}x", $"{vertKey.instanced}", $"{Path.GetFileNameWithoutExtension(vertKey.cacheKey)}", $"{fragKey.instanced}", $"{Path.GetFileNameWithoutExtension(fragKey.cacheKey)}"), LogSource.System);
            }

            //public static Dictionary<(int vertId, int fragId), (int id, int users)> Programs = new();
            //Path.GetFileNameWithoutExtension(path);

            /*
            foreach (var entry in GraphicsCache.Programs.OrderByDescending(e => e.Value.users))
                Logger.Log(string.Format(spacing, $"{entry.Value.users}/{Taken.ProgramUsers} Program Users", $"{entry.Key}"), LogSource.System);
            */
        
        /*

        sb.AppendLine($"Unique Textures - {Textures.Count}x");
        foreach (var entry in Textures)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Vertex Shaders - {Vertex.Count}x");
        foreach (var entry in Vertex)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Fragment Shaders - {Fragment.Count}x");
        foreach (var entry in Fragment)
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");

        sb.AppendLine($"Unique Shader Programs - {Programs.Count}x");
        int totalProgramUsers = 0;
        foreach (var entry in Programs)
        {
            sb.AppendLine($" {entry.Value.users}x {entry.Key}");
            totalProgramUsers += entry.Value.users;
        }
        */
            Logger.Log("--------------------------", LogSource.System);

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

    static string ToFloatDiff(float amount)
    {
        return amount >= 0 ? $"+{amount:F2}" : $"-{-amount:F2}";
    }

    static string ToCountDiff(int amount)
    {
        return amount >= 0 ? $"+{amount}x" : $"-{-amount}x";
    }
}