using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Crux.Core;

public static class Input
{
    private static Dictionary<string, Keys> keybindings = [];

    public static void CreateAction(string action, Keys key)
    {
        action = action.ToUpper();

        if (keybindings.ContainsKey(action))
            Logger.LogWarning($"Action '{action}' already bound to key {key}.");
        else
            keybindings[action] = key;
    }

    public static void OutputKeyBindings()
    {
        Logger.Log("KEY\t\tACTION");
        foreach (var pair in keybindings)
            Logger.Log($"{pair.Value.ToString().ToUpper()}\t\t{pair.Key}");
    }

    public static bool IsActionHeld(string action)
    {
        action = action.ToUpper();

        if (keybindings.ContainsKey(action))
        {
            return GameEngine.Link.IsKeyDown(keybindings[action]);
        }else
        {
            Logger.LogWarning($"Action '{action}' is unbound.");
            return false;
        }        
    }
    
    public static bool IsActionPressed(string action)
    {
        action = action.ToUpper();

        if (keybindings.ContainsKey(action))
        {
            return GameEngine.Link.IsKeyPressed(keybindings[action]);
        }else
        {
            Logger.LogWarning($"Action '{action}' is unbound.");
            return false;
        }
    }

    public static bool IsActionReleased(string action)
    {
        action = action.ToUpper();

        if (keybindings.ContainsKey(action))
        {
            return GameEngine.Link.IsKeyReleased(keybindings[action]);
        }else
        {
            Logger.LogWarning($"Action '{action}' is unbound.");
            return false;
        }
    }
}
