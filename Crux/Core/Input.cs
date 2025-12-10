using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Crux.Core;

public static class Input
{
    private static Dictionary<string, (Keys key, bool permanent)> keybindings = [];

    public static void CreateAction(string action, Keys key, bool permanent = false)
    {
        action = action.ToUpper();

        if (keybindings.ContainsKey(action))
            Logger.LogWarning($"Action '{action}' already bound to key '{key}'{(keybindings[action].permanent ? " permanently" : "")}.");
        else
            keybindings[action] = (key, permanent);
    }

    public static void UnbindAll()
    {
        foreach (var key in keybindings.Where(pair => !pair.Value.permanent).Select(pair => pair.Key).ToList())
            keybindings.Remove(key);
    }

    public static void OutputKeyBindings()
    {
        Logger.Log(string.Format("{0,-14}{1}", "KEY", "ACTION"));

        foreach (var pair in keybindings)
            Logger.Log(string.Format("{0,-14}{1}",
                                    pair.Value.key.ToString().ToUpper(),
                                    pair.Key));
    }

    public static bool IsActionHeld(string action)
    {
        action = action.ToUpper();

        if (keybindings.ContainsKey(action))
        {
            return GameEngine.Link.IsKeyDown(keybindings[action].key);
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
            return GameEngine.Link.IsKeyPressed(keybindings[action].key);
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
            return GameEngine.Link.IsKeyReleased(keybindings[action].key);
        }else
        {
            Logger.LogWarning($"Action '{action}' is unbound.");
            return false;
        }
    }
}
