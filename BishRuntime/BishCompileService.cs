using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;

namespace BishRuntime;

using Parser = Func<ICodeSource, CompilerResult<BishParseTree>>;
using Compiler = Func<CompilerResult<BishParseTree>, CompileOptions, CompilerResult<IList<BishBytecode>>>;

public record CompileOptions(bool Optimize = true, bool Throws = true)
{
    public BishMap Map => new([
        new Entry(new BishString("optimize"), BishBool.Of(Optimize)),
        new Entry(new BishString("throws"), BishBool.Of(Throws))
    ]);
}

public class BishLanguage(Parser parser, Compiler compiler) : BishObject
{
    public Parser Parser => parser;
    public Compiler Compiler => compiler;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Language");

    // TODO: recording errors
    [Builtin("hook")]
    public static BishLanguage New(BishObject parser, BishObject compiler) => new(
        code => new CompilerResult<BishParseTree>(
            parser.Call(new BishArgs([new BishCodeSource(code)])).As<BishParseTree>("result"), []),
        (result, options) => new CompilerResult<IList<BishBytecode>>(
            compiler.Call(new BishArgs([result.Result, options.Map])).As<BishList>("bytecodes").List.Select(item =>
                BishBytecodeParser.FromObject(item.As<BishBytecodeObject>("bytecode"))).ToList(), []));
}

public static class BishCompileService
{
    public static readonly IDictionary<string, BishLanguage> Languages =
        new ConcurrentDictionary<string, BishLanguage>();

    private static BishLanguage Language(string lang) => Languages.TryGetValue(lang, out var language)
        ? language
        : throw BishException.OfCompile_InvalidLang(lang);

    public static BishFrame Compile(ICodeSource source, BishScope? scope = null, CompileOptions? options = null)
    {
        var frame = Compile(source, out var errors, scope, options);
        CheckErrors(errors);
        return frame;
    }

    public static BishFrame Compile(ICodeSource source, out IList<CompilationError> errors,
        BishScope? scope = null, CompileOptions? options = null)
    {
        var ext = source.Extension;
        if (ext == "bishc")
        {
            using var stream = File.OpenRead(source.Filename);
            errors = [];
            var value = stream.ReadBytecodes();
            if (scope is not null) value.Scope = scope;
            return value.AddMeta(source.Root);
        }

        var language = Language(ext);
        var result = language.Compiler(language.Parser(source), options ?? new CompileOptions());
        errors = result.Errors;
        var frame = new BishFrame(result.Result, scope).AddMeta(source.Root).WithSource(source);
        return frame;
    }

    public static BishFrame Compile(string lang, BishParseTree obj)
    {
        var result = Language(lang).Compiler(new CompilerResult<BishParseTree>(obj, []), new CompileOptions());
        CheckErrors(result.Errors);
        return new BishFrame(result.Result).AddMeta(null);
    }

    public static BishParseTree Parse(string lang, string code)
    {
        var result = Language(lang).Parser(new VirtualSource("<code>", lang, code));
        CheckErrors(result.Errors);
        return result.Result;
    }

    private static BishFrame AddMeta(this BishFrame frame, string? root)
    {
        frame.Scope.DefVar("meta", new BishMeta(root ?? Environment.CurrentDirectory));
        return frame;
    }

    public static void CheckErrors(IList<CompilationError> errors)
    {
        if (errors.Count > 0) throw BishException.OfCompile_Errors(errors);
    }

    static BishCompileService()
    {
        BishMeta.Builtin.Root = Environment.CurrentDirectory;
        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "*Language.dll"))
        foreach (var lang in Assembly.LoadFrom(file).TypesOf(typeof(ILanguage)))
            ILanguage.Register(lang);
    }
}

public interface ICodeSource
{
    public string Filename { get; }
    public string Code { get; }
    public string? Root => null;
    public string Extension { get; }
}

public record FileSource(string Name) : ICodeSource
{
    public string Filename => Path.GetFullPath(Name);

    public string Code => File.Exists(Filename)
        ? File.ReadAllText(Filename)
        : throw BishException.OfCompile_NoFile(Filename);

    public string? Root
    {
        get
        {
            var path = Path.GetDirectoryName(Filename);
            if (path is null) return null;
            var current = new DirectoryInfo(path);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "rubbish.json")))
                    return current.FullName;
                current = current.Parent;
            }

            return Path.GetDirectoryName(path);
        }
    }

    public string Extension => Path.GetExtension(Filename)[1..];
}

public record VirtualSource(string Filename, string Extension, string Code) : ICodeSource;

public class BishCodeSource(ICodeSource source) : BishObject
{
    public readonly ICodeSource Source = source;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("CodeSource");

    [Builtin]
    public static BishCodeSource File(BishString name) => new(new FileSource(name.Value));

    [Builtin]
    public static BishCodeSource Virtual(BishString name, BishString ext, BishString code) =>
        new(new VirtualSource(name.Value, ext.Value, code.Value));

    [Builtin]
    public static BishCodeSource Code(BishString ext, BishString code) => Virtual(new BishString("<code>"), ext, code);
}

public record SourcePosition(int Line, int Column, int StopLine, int StopColumn)
{
    public override string ToString() => $"line {Line}, column {Column} to line {StopLine}, column {StopColumn}";

    public static SourcePosition? Combine(params IEnumerable<SourcePosition?> positions)
    {
        var pos = positions.OfType<SourcePosition>().ToArray();
        if (pos.Length == 0) return null;
        var min = pos.Select(p => (p.Line, p.Column)).Min();
        var max = pos.Select(p => (p.StopLine, p.StopColumn)).Max();
        return new SourcePosition(min.Line, min.Column, max.StopLine, max.StopColumn);
    }

    public string Slice(string source)
    {
        var lines = source.Split('\n');
        if (Line == StopLine) return lines[Line - 1][Column..(StopColumn + 1)];
        var sb = new StringBuilder();
        sb.AppendLine(lines[Line - 1][Column..]);
        for (var i = Line; i < StopLine - 1; i++) sb.AppendLine(lines[i]);
        sb.Append(lines[StopLine - 1][..(StopColumn + 1)]);
        return sb.ToString();
    }

    public BishList ToObject() =>
        new(new[] { Line, Column, StopLine, StopColumn }.Select(BishInt.Of).ToList<BishObject>());

    public static SourcePosition FromObject(BishList list) => new(
        list.Index(0).As<BishInt>("line").Value, list.Index(1).As<BishInt>("column").Value,
        list.Index(2).As<BishInt>("stopLine").Value, list.Index(3).As<BishInt>("stopColumn").Value);
}

public record CompilationError(SourcePosition Position, string Message)
{
    public string? File = null;
    
    public override string ToString() => $"Compilation error: {Message}, at {File}, {Position}";

    public BishError ToError() => BishException.OfCompile(ToString())
        .With("pos", Position.ToObject())
        .With("file", File is null ? BishNull.Instance : new BishString(File))
        .With("info", new BishString(Message)).Error;
}

public record CompilerResult<T>(T Result, IList<CompilationError> Errors);

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ILanguage
{
    [UsedImplicitly] static abstract string Name { get; }
    [UsedImplicitly] static abstract BishLanguage Language { get; }

    internal static void Register(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        var name = (string)type.GetProperty("Name", flags)!.GetValue(null)!;
        var lang = (BishLanguage)type.GetProperty("Language", flags)!.GetValue(null)!;
        BishCompileService.Languages[name] = lang;
    }
}