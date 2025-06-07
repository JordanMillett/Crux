using Crux.Utilities.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Runtime.CompilerServices;

namespace Crux.Scripting;

public class SandboxContext
{
    public Dictionary<string, object> Bound { get; }

    public SandboxContext()
    {
        Bound = new Dictionary<string, object>();  
    }
}

public class Sandbox
{
    SandboxContext Context { get; } = new SandboxContext();
    
    public Sandbox(Dictionary<string, object>? initialContext = null)
    {
        if(initialContext != null)
        {
            foreach (var pair in initialContext)
                Context.Bound[pair.Key] = pair.Value;
        }
        
        UpdateContext((Action)OutputFullContext, "OutputFullContext");
    }

    public void UpdateContext(object obj, [CallerArgumentExpression("obj")] string? key = null)
    {
        if (obj == null)
            return;

        string contextKey = key ?? obj.GetType().Name;
        Context.Bound[contextKey] = obj;
    }

    public void OutputFullContext()
    {
        Logger.Log("---Scripting Sandbox Full Context---");

        Logger.Log($"(Static Class) Logger");

        foreach (var pair in Context.Bound)
        {
            Logger.Log($"({pair.Value?.GetType().Name}) {pair.Key}");
        }
    }

    public async Task ExecuteScriptAsync(string path)
    {
        string code = AssetHandler.ReadScriptInFull(path);

        ScriptOptions options = ScriptOptions.Default
            .WithReferences(typeof(object).Assembly, typeof(Logger).Assembly)
            .WithImports("System");

        try
        {
            await CSharpScript.EvaluateAsync(code, options, Context);
        }
        catch (CompilationErrorException e)
        {
            Logger.LogWarning($"{path} Compilation Error.");
            Logger.LogError(e);
        }
        catch (Exception e)
        {
            Logger.LogWarning($"{path} Runtime Error.");
            Logger.LogError(e);
        }
    }
}
