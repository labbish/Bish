using System.Reflection;

namespace BishRuntime;

public class BishMeta(string? root) : BishObject
{
    public string? Root = root;

    public static string LibRoot => Path.Combine(AppContext.BaseDirectory, "lib");

    public static readonly List<string> Extensions = [".dll", ".bishc", ".bish"];

    public static BishMeta Builtin => new(null);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("meta");

    [Builtin("hook")]
    public static BishString? Get_root(BishMeta self) => self.Root is null ? null : new BishString(self.Root);

    [Builtin("hook")]
    public static void Set_root(BishMeta self, BishObject value) =>
        self.Root = value is BishNull ? null : value.As<BishString>("meta.root").Value;

    [Builtin("hook")]
    public static BishString Get_libRoot(BishMeta _) => new(LibRoot);

    [Builtin("hook")]
    public static BishList Get_extensions(BishMeta _) =>
        new(Extensions.Select(ext => new BishString(ext)).ToList<BishObject>());

    [Builtin("hook")]
    public static BishProxyMap Get_cache(BishMeta _) => new(BishImporter.Cache);

    [Builtin]
    public static BishObject Parse(BishMeta _, BishString code) => BishCompileService.Parse(code.Value);

    [Builtin]
    public static BishFrame Compile(BishMeta _, BishObject obj) => obj is BishString code
        ? BishCompileService.Compile(new VirtualSource("<string>", code.Value))
        : BishCompileService.Compile(obj);

    [Builtin]
    public static BishFrame CompileFile(BishMeta _, BishString path) =>
        BishCompileService.Compile(new FileSource(path.Value));
}

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

    private static BishObject ImportDll(string path)
    {
        var assembly = Assembly.LoadFrom(path);
        var all = assembly.GetTypes();
        var types = all.Where(type => type is { IsClass: true, IsAbstract: false, IsPublic: true }
                                      && typeof(IPlugin).IsAssignableFrom(type)).ToList();
        if (types.Count == 0) throw BishException.OfImport_Dll(path, all);
        var module = new BishObject();
        foreach (var type in types)
        {
            var plugin = Activator.CreateInstance(type) as IPlugin;
            var exports = new PluginExports();
            plugin?.Initialize(exports);
            foreach (var (name, value) in exports.Exports)
                module.DefMember(name, value);
        }

        return module;
    }
}

public class PluginExports
{
    // ReSharper disable once CollectionNeverUpdated.Global
    public readonly Dictionary<string, BishObject> Exports = [];
}

public interface IPlugin
{
    void Initialize(PluginExports exports);
}