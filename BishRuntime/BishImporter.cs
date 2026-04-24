using System.Reflection;

namespace BishRuntime;

public class BishMeta(string? root) : BishObject
{
    public string? Root = root;

    public static BishMeta Default => new(null);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("meta");

    [Builtin("hook")]
    public static BishString? Get_root(BishMeta self) => self.Root is null ? null : new BishString(self.Root);

    [Builtin("hook")]
    public static void Set_root(BishMeta self, BishObject value) =>
        self.Root = value is BishNull ? null : value.As<BishString>("meta.root").Value;

    // TODO: compile method
}

public static class BishImporter
{
    private static readonly Dictionary<string, BishObject> Cache = [];

    public static BishObject Import(BishMeta meta, string file)
    {
        var root = meta.Root;
        if (BishScope.BuiltinModules.TryGetValue(file, out var module)) return module;
        try
        {
            var path = Path.GetFullPath(root is null ? file : Path.Combine(root, file));
            if (Cache.TryGetValue(path, out var cached)) return cached;
            var result = ImportFull(path);
            Cache.Add(path, result);
            return result;
        }
        catch (BishException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw BishException.OfImport(file, e.ToString());
        }
    }

    private static BishObject ImportFull(string path)
    {
        var ext = Path.GetExtension(path);
        switch (ext)
        {
            case ".bish": return RunAndCopy(BishCompileService.CompileFile(path));
            case ".bishc":
            {
                using var stream = File.OpenRead(path);
                return RunAndCopy(new BishFrame(stream.ReadBytecodes()));
            }
            case ".dll": return ImportDll(path);
            default: throw new ArgumentException($"Invalid file extension: {ext}");
        }
    }

    private static BishObject RunAndCopy(BishFrame frame)
    {
        frame.Execute();
        var module = new BishObject();
        foreach (var (name, value) in frame.Scope.Vars) module.DefMember(name, value);
        return module;
    }

    private static BishObject ImportDll(string path)
    {
        var assembly = Assembly.LoadFrom(path);
        var types = assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false, IsPublic: true } &&
            typeof(IPlugin).IsAssignableFrom(type)).ToList();
        if (types.Count == 0)
            throw new ArgumentException($"Cannot find plugin initializer in {path}: " +
                                        $"found types {string.Join(", ", assembly.GetTypes())}, none of which implements IPlugin");
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