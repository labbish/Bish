using System.Text;

namespace BishRuntime;

public record CompileOptions(bool Optimize = true, bool Throws = true);

public static class BishCompileService
{
    public static Func<string, CompilerResult<BishObject>> Parser
    {
        get => field ?? throw BishException.OfCompile_NoService();
        set;
    }

    public static Func<CompilerResult<BishObject>, CompileOptions, CompilerResult<IList<BishBytecode>>> Compiler
    {
        get => field ?? throw BishException.OfCompile_NoService();
        set;
    }

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
        switch (ext)
        {
            case ".bishc":
            {
                using var stream = File.OpenRead(source.Filename);
                errors = [];
                var value = stream.ReadBytecodes();
                if (scope is not null) value.Scope = scope;
                return value.AddMeta(source.Root);
            }
            case ".bish" or null:
            {
                var result = Compiler(Parser(source.Code), options ?? new CompileOptions());
                errors = result.Errors;
                var frame = new BishFrame(result.Result, scope).AddMeta(source.Root).WithSource(source);
                return frame;
            }
            default: throw BishException.OfCompile_InvalidExt(ext);
        }
    }

    public static BishFrame Compile(BishObject obj)
    {
        var result = Compiler(new CompilerResult<BishObject>(obj, []), new CompileOptions());
        CheckErrors(result.Errors);
        return new BishFrame(result.Result).AddMeta(null);
    }

    public static BishObject Parse(string code)
    {
        var result = Parser(code);
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

    static BishCompileService() => BishMeta.Builtin.Root = Environment.CurrentDirectory;
}

public interface ICodeSource
{
    public string Filename { get; }
    public string Code { get; }
    public string? Root => null;
    public string? Extension => null;
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

    public string Extension => Path.GetExtension(Filename);
}

public record VirtualSource(string Filename, string Code) : ICodeSource;

public class BishCodeSource(ICodeSource source) : BishObject
{
    public readonly ICodeSource Source = source;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("CodeSource");

    [Builtin]
    public static BishCodeSource File(BishString name) => new(new FileSource(name.Value));

    [Builtin]
    public static BishCodeSource Virtual(BishString name, BishString code) =>
        new(new VirtualSource(name.Value, code.Value));

    [Builtin]
    public static BishCodeSource Code(BishString code) => new(new VirtualSource("<code>", code.Value));
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
}

public record CompilationError(SourcePosition Position, string Message)
{
    public override string ToString() => $"Compilation error at {Position}: {Message}";

    public BishError ToError() => BishException.OfCompile(ToString())
        .With("start", new BishList([BishInt.Of(Position.Line), BishInt.Of(Position.Column)]))
        .With("end", new BishList([BishInt.Of(Position.StopLine), BishInt.Of(Position.StopColumn)]))
        .With("info", new BishString(Message)).Error;
}

public record CompilerResult<T>(T Result, IList<CompilationError> Errors);