namespace BishRuntime;

public class BishMeta(string? root) : BishObject
{
    public string? Root = root;

    public static string LibRoot => Path.Combine(AppContext.BaseDirectory, "lib");

    public static List<string> Extensions => ["dll", "bishc", ..BishCompileService.Languages.Keys];

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

    [Builtin("hook")]
    public static BishProxyMap<BishLanguage> Get_languages(BishMeta _) => new(BishCompileService.Languages);

    [Builtin("hook")]
    public static BishType Get_Language(BishObject _) => BishLanguage.StaticType;

    [Builtin]
    public static BishObject Parse(BishMeta _, BishString lang, BishString code) =>
        BishCompileService.Parse(lang.Value, code.Value);

    [Builtin]
    public static BishFrame Compile(BishMeta _, BishCodeSource source, [DefaultNull] BishScope? scope) =>
        BishCompileService.Compile(source.Source, scope);

    [Builtin]
    public static BishFrame CompileParsed(BishMeta _, BishString lang, BishParseTree tree) =>
        BishCompileService.Compile(lang.Value, tree);
}