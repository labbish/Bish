using Antlr4.Runtime;
using BishBytecode;

namespace BishCompiler;

public static class BishCompiler
{
    public static BishFrame Compile(string code, BishScope? scope = null,
        ErrorHandlingMode mode = ErrorHandlingMode.Default)
    {
        var stream = CharStreams.fromString(code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);
        if (mode != ErrorHandlingMode.Default)
        {
            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            IAntlrErrorListener<int> listener = mode switch
            {
                ErrorHandlingMode.Ignore => new IgnoreErrorListener(),
                ErrorHandlingMode.Throw => new ThrowingErrorListener(),
                _ => throw new ArgumentException("Invalid mode")
            };
            lexer.AddErrorListener(listener);
        }

        var tree = parser.program();
        var visitor = new BishVisitor();
        var codes = visitor.Visit(tree);
        return new BishFrame(codes, scope);
    }
}

public enum ErrorHandlingMode
{
    Default,
    Ignore,
    Throw
};

public class ThrowingErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e) =>
        throw new ArgumentException($"Found error at line {line}, row {charPositionInLine}: {msg}", e);
}

public class IgnoreErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e)
    {
    }
}