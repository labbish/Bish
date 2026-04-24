global using Codes = System.Collections.Generic.IList<BishRuntime.BishBytecode>;
global using Antlr4.Runtime.Tree;
global using Antlr4.Runtime;
global using BishRuntime;
using System.Runtime.CompilerServices;
using BishUtils;

namespace BishCompiler;

public static class BishCompiler
{
    private static (IList<CompilationError>, Codes) Compile(string code, CompileOptions options)
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

        var program = parser.program();
        var visitor = new BishVisitor();
        var result = visitor.VisitFull(program, optimize: options.Optimize);
        
        if (options.Throws && result.Errors.Count > 0)
            throw new Exception("Crucial compile error(s) occured: " + string.Join("\n", result.Errors));
        var errors = (ConcurrentList<CompilationError>)[..listener.Errors, ..result.Errors];
        return (errors, result.Codes);
    }

    public static void Init() => RuntimeHelpers.RunClassConstructor(typeof(BishCompiler).TypeHandle);

    static BishCompiler()
    {
        BishLib.BishLib.Initialize();
        BishCompileService.Compiler = Compile;
    }
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