global using Codes = System.Collections.Generic.IList<BishRuntime.BishBytecode>;
global using Antlr4.Runtime.Tree;
global using Antlr4.Runtime;
global using BishRuntime;
using BishUtils;

namespace BishCompiler;

public record CompileOptions(bool Optimize = true, bool Throws = true);

public static class BishCompiler
{
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

    public static BishFrame SimpleCompileFile(string path) => CompileFile(path);

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
        options ??= new CompileOptions();
        
        var stream = CharStreams.fromString(code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);

        var listener = new ErrorListener();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var program = parser.program();
        var visitor = new BishVisitor();
        var result = visitor.VisitFull(program, optimize: options.Optimize);

        if (options.Throws && result.Errors.Count > 0)
            throw new Exception("Crucial compile error(s) occured: " + string.Join("\n", result.Errors));
        errors = (ConcurrentList<CompilationError>)[..listener.Errors, ..result.Errors];

        return new BishFrame(result.Codes, scope).AddMeta(root);
    }

    public static BishFrame AddMeta(this BishFrame frame, string? root)
    {
        frame.Scope.DefVar("meta", new BishMeta(root ?? Environment.CurrentDirectory, SimpleCompileFile));
        return frame;
    }
    
    static BishCompiler()
    {
        BishLib.BishLib.Initialize();
        BishMeta.Default.Root = Environment.CurrentDirectory;
        BishMeta.Default.CompileFile = SimpleCompileFile;
    }
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

public class ErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public IList<CompilationError> Errors { get; } = new ConcurrentList<CompilationError>();

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var length = offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1;
        var stopColumn = charPositionInLine + length;
        Errors.Add(new CompilationError(line, charPositionInLine, msg, line, stopColumn));
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Errors.Add(new CompilationError(line, charPositionInLine, msg, line, charPositionInLine + 1));
    }
}