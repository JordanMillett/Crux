global using OpenTK.Mathematics;
global using System.Text;
global using CruxEngine.Core;

namespace CruxEngine;

public static class Crux
{
    public static GameEngine Engine { get; internal set; }
    public static CruxEngine.Components.CameraComponent Camera => Engine.Camera!;
}
