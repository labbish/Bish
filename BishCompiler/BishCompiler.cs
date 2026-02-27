using Antlr4.Runtime;
using BishBytecode;

namespace BishCompiler;

public static class BishCompiler
{
    public static BishParser.ProgramContext Parse(string code, out List<SyntaxError> errors)
    {
        var stream = CharStreams.fromString(code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);

        var listener = new SyntaxErrorListener();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var result = parser.program();
        errors = listener.Errors;
        return result;
    }

    public static BishFrame Compile(string code, BishScope? scope = null, bool optimize = true) =>
        Compile(code, out _, scope, optimize);

    public static BishFrame Compile(string code, out List<SyntaxError> errors,
        BishScope? scope = null, bool optimize = true)
    {
        var tree = Parse(code, out var list);
        errors = list;
        var visitor = new BishVisitor();
        var codes = visitor.VisitFull(tree, optimize: optimize);
        return new BishFrame(codes, scope);
    }
}

public record SyntaxError(int Line, int Column, string Message, int StopLine, int StopColumn)
{
    public override string ToString() => $"Syntax error at line {Line}, column {Column}: {Message}";
}

public class SyntaxErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public List<SyntaxError> Errors { get; } = [];

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var length = offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1;
        var stopColumn = charPositionInLine + length;
        Errors.Add(new SyntaxError(line, charPositionInLine, msg, line, stopColumn));
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Errors.Add(new SyntaxError(line, charPositionInLine, msg, line, charPositionInLine + 1));
    }
}