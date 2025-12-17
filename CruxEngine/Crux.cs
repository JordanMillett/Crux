global using OpenTK.Mathematics;
global using System.Text;
global using CruxEngine.Core;

namespace CruxEngine;

public static class Crux
{
    public static GameEngine Engine { get; internal set; } = null!;
    public static CruxEngine.Components.CameraComponent Camera => Engine.Camera!;
    public static CruxEngine.Components.CanvasComponent? Canvas = null!;
}
