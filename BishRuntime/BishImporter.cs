using System.Reflection;
using JetBrains.Annotations;

namespace BishRuntime;

public static class BishImporter
{
    public static readonly Dictionary<string, BishObject> Cache = [];

    public static BishObject Import(BishMeta? meta, string file)
    {
        if (BishScope.BuiltinModules.TryGetValue(file, out var module)) return module;
        var path = Locate(meta?.Root ?? "", file);
        if (Cache.TryGetValue(path, out var cached)) return cached;
        var result = ImportFull(path);
        Cache.Add(path, result);
        return result;
    }

    private static string Locate(string root, string file)
    {
        var exts = BishMeta.Extensions;
        var ext = Path.GetExtension(file);
        if (ext != "" && !exts.Contains(ext)) throw BishException.OfImport_InvalidExt(file, ext);
        foreach (var path in new[] { root, BishMeta.LibRoot })
        {
            var full = Path.Combine(path, file);
            if (ext != "" && File.Exists(full)) return full;
            var found = exts.Select(e => full + e).FirstOrDefault(File.Exists);
            if (found is not null) return found;
        }

        throw BishException.OfImport_NoFile(file);
    }

    private static BishObject ImportFull(string path)
    {
        var ext = Path.GetExtension(path);
        if (ext == ".dll") return ImportDll(path);
        var frame = BishCompileService.Compile(new FileSource(path));
        frame.Execute();
        var module = new BishObject();
        foreach (var (name, value) in frame.Scope.Vars) module.DefMember(name, value);
        return module;
    }

    private static BishObject ImportDll(string path) =>
        IModule.TypesFromAssembly(Assembly.LoadFrom(path)) is [var module]
            ? IModule.ExportsFromType(module)
            : throw BishException.OfImport_Dll(path);
}

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IModule
{
    [UsedImplicitly]
    static abstract BishObject Exports { get; }

    protected static BishObject ExportsFrom(params IEnumerable<(string, BishObject)> exports)
    {
        var module = new BishObject();
        foreach (var (key, value) in exports)
            module.DefMember(key, value);
        return module;
    }

    public static Type[] TypesFromAssembly(Assembly assembly) => assembly.GetTypes().Where(type =>
        type is { IsAbstract: false, IsPublic: true } && typeof(IModule).IsAssignableFrom(type)).ToArray();

    public static BishObject ExportsFromType(Type type) => (BishObject)type.GetProperty("Exports",
        BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)!.GetValue(null)!;
}