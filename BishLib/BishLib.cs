using BishRuntime;

namespace BishLib;

public static class BishLib
{
    public static void Initialize()
    {
        BishThreadModule.Initialize();
        BishFileModule.Initialize();
        BishRandomModule.Initialize();
        BuiltinFunctionRegistry.Registry();
    }

    internal static void InitializeModule(string name, params IEnumerable<(string, BishObject)> exports)
    {
        var module = new BishObject();
        foreach (var (key, value) in exports)
            module.DefMember(key, value);
        BishScope.BuiltinModules.Add(name, module);
    }
}