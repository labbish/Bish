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
    public static BishFrame Compile(BishMeta _, BishObject obj, [DefaultNull] BishScope? scope) => obj switch
    {
        BishString code => BishCompileService.Compile(new VirtualSource("<string>", code.Value), scope),
        BishCodeSource source => BishCompileService.Compile(source.Source, scope),
        _ => BishCompileService.Compile(obj)
    };

    [Builtin]
    public static BishFrame CompileFile(BishMeta _, BishString path, [DefaultNull] BishScope? scope) =>
        BishCompileService.Compile(new FileSource(path.Value), scope);
}