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

    private static string? FindRoot(string? path)
    {
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

    public static BishFrame CompileFile(string file, BishScope? scope = null, CompileOptions? options = null)
    {
        var frame = CompileFile(file, out var errors, scope, options);
        CheckErrors(errors);
        return frame;
    }

    public static BishFrame CompileFile(string file, out IList<CompilationError> errors,
        BishScope? scope = null, CompileOptions? options = null)
    {
        var path = Path.GetFullPath(file);
        var ext = Path.GetExtension(path);
        var root = FindRoot(Path.GetDirectoryName(path));
        if (!File.Exists(path)) throw BishException.OfCompile_NoFile(path);
        switch (ext)
        {
            case ".bish": return Compile(File.ReadAllText(path), out errors, root, scope, options);
            case ".bishc":
            {
                using var stream = File.OpenRead(path);
                errors = [];
                return new BishFrame(stream.ReadBytecodes(), scope).AddMeta(root);
            }
            default: throw BishException.OfCompile_InvalidExt(ext);
        }
    }

    public static BishFrame Compile(string code, string? root = null,
        BishScope? scope = null, CompileOptions? options = null)
    {
        var frame = Compile(code, out var errors, root, scope, options);
        CheckErrors(errors);
        return frame;
    }

    public static BishFrame Compile(string code, out IList<CompilationError> errors,
        string? root = null, BishScope? scope = null, CompileOptions? options = null)
    {
        var result = Compiler(Parser(code), options ?? new CompileOptions());
        errors = result.Errors;
        return new BishFrame(result.Result, scope).AddMeta(root);
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

public record SourcePosition(int Line, int Column, int StopLine, int StopColumn)
{
    public override string ToString() => $"line {Line}, column {Column} to line {StopLine}, column {StopColumn}";

    public string ShortString() => (Line == StopLine, Column == StopColumn) switch
    {
        (true, true) => $"{Line}:{Column}",
        (true, _) => $"{Line}:{Column}~{StopColumn}",
        _ => $"{Line}:{Column}~{StopLine}:{StopColumn}"
    };

    public static SourcePosition? Combine(params IEnumerable<SourcePosition?> positions)
    {
        var pos = positions.OfType<SourcePosition>().ToArray();
        if (pos.Length == 0) return null;
        var min = pos.Select(p => (p.Line, p.Column)).Min();
        var max = pos.Select(p => (p.StopLine, p.StopColumn)).Max();
        return new SourcePosition(min.Line, min.Column, max.StopLine, max.StopColumn);
    }
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