using Antlr4.Runtime;
using BishBytecode;
using BishRuntime;

namespace BishCompiler;

public static class BishCompiler
{
    public static BishFrame Compile(string code, out List<CompilationError> errors,
        BishScope? scope = null, string? root = null, bool optimize = true, bool runs = true)
    {
        var stream = CharStreams.fromString(code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);

        var listener = new ErrorListener();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var result = parser.program();
        var visitor = new BishVisitor();
        var codes = visitor.VisitFull(result, optimize: optimize);

        if (runs && visitor.Errors.Count > 0)
            throw new Exception("Crucial compile error(s) occured: " + string.Join("\n", visitor.Errors));
        errors = [..listener.Errors, ..visitor.Errors];

        var frame = new BishFrame(codes, scope);
        frame.Scope.DefVar("import", new BishFunc("import", [new BishArg("file", BishString.StaticType)], args =>
        {
            var path = "(unresolved)";
            try
            {
                var file = args[0].ExpectToBe<BishString>("file");
                path = Path.GetFullPath(root is null ? file.Value : Path.Combine(root, file.Value));
                var imported = Compile(File.ReadAllText(path), out var errors);
                if (errors.Count != 0) throw new ArgumentException(errors[0].Message);
                imported.Execute();
                var module = new BishObject();
                foreach (var (name, value) in imported.Scope.Vars)
                    module.SetMember(name, value);
                return module;
            }
            catch (BishException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw BishException.OfImport(e.Message, path);
            }
        }));
        return frame;
    }
}

public record CompilationError(int Line, int Column, string Message, int StopLine, int StopColumn)
{
    public override string ToString() => $"Compilation error at line {Line}, column {Column}: {Message}";
}

public class ErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public List<CompilationError> Errors { get; } = [];

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