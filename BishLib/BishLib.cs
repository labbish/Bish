using BishRuntime;
using BishUtils;

namespace BishLib;

public static class BishLib
{
    public static void Initialize()
    {
        foreach (var module in typeof(BishLib).Assembly.TypesOf(typeof(IModule)))
            BishScope.BuiltinModules.Add(ModuleName(module.Name), IModule.ExportsFromType(module));
        BuiltinsRegistry.Register();
    }

    private static string ModuleName(string cls) => cls.RemoveStart("Bish").RemoveEnd("Module").ToLowerInvariant();
}