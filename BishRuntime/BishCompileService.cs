using System.Diagnostics.CodeAnalysis;

namespace BishRuntime;

public record CompileOptions(bool Optimize = true, bool Throws = true);

public static class BishCompileService
{
    [SuppressMessage("Usage", "CA2211")]
    public static Func<string, CompileOptions, (IList<CompilationError>, IList<BishBytecode>)>? Compiler;

    public static string? FindRoot(string? path)
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

    public static BishFrame CompileFile(string path, BishScope? scope = null, CompileOptions? options = null)
    {
        var frame = CompileFile(path, out var errors, scope, options);
        return errors.Count != 0 ? throw new ArgumentException($"Compilation Error: {errors[0]}") : frame;
    }

    public static BishFrame CompileFile(string path, out IList<CompilationError> errors,
        BishScope? scope = null, CompileOptions? options = null)
    {
        path = Path.GetFullPath(path);
        var ext = Path.GetExtension(path);
        var root = FindRoot(Path.GetDirectoryName(path));
        switch (ext)
        {
            case ".bish": return Compile(File.ReadAllText(path), out errors, root, scope, options);
            case ".bishc":
            {
                using var stream = File.OpenRead(path);
                errors = [];
                return new BishFrame(stream.ReadBytecodes(), scope).AddMeta(root);
            }
            default: throw new ArgumentException($"Invalid file extension: {ext}");
        }
    }

    public static BishFrame Compile(string code, string? root = null,
        BishScope? scope = null, CompileOptions? options = null)
    {
        var frame = Compile(code, out var errors, root, scope, options);
        return errors.Count != 0 ? throw new ArgumentException($"Compilation Error: {errors[0]}") : frame;
    }

    public static BishFrame Compile(string code, out IList<CompilationError> errors,
        string? root = null, BishScope? scope = null, CompileOptions? options = null)
    {
        if (Compiler is null) throw new ArgumentException("Compile service is invalid!");
        var (e, codes) = Compiler(code, options ?? new CompileOptions());
        errors = e;
        return new BishFrame(codes, scope).AddMeta(root);
    }

    public static BishFrame AddMeta(this BishFrame frame, string? root)
    {
        frame.Scope.DefVar("meta", new BishMeta(root ?? Environment.CurrentDirectory));
        return frame;
    }

    static BishCompileService() => BishMeta.Default.Root = Environment.CurrentDirectory;
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
}