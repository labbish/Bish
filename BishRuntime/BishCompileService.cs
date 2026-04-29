using System.Diagnostics.CodeAnalysis;

namespace BishRuntime;

public record CompileOptions(bool Optimize = true, bool Throws = true);

public static class BishCompileService
{
    [SuppressMessage("Usage", "CA2211")]
    public static Func<string, CompileOptions, (IList<CompilationError>, IList<BishBytecode>)>? Compiler;

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
        if (Compiler is null) throw BishException.OfCompile_NoService();
        var (e, codes) = Compiler(code, options ?? new CompileOptions());
        errors = e;
        return new BishFrame(codes, scope).AddMeta(root);
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

public record CompilationError(
    int Line,
    int Column,
    string Message,
    int StopLine,
    int StopColumn)
{
    public override string ToString() =>
        $"Compilation error at line {Line}, column {Column} to line {StopLine}, column {StopColumn}: {Message}";

    public BishError ToError() => BishException.OfCompile(ToString())
        .With("start", new BishList([BishInt.Of(Line), BishInt.Of(Column)]))
        .With("end", new BishList([BishInt.Of(StopLine), BishInt.Of(StopColumn)]))
        .With("info", new BishString(Message)).Error;
}