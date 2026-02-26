using Antlr4.Runtime;
using BishBytecode;

namespace BishCompiler;

public static class BishCompiler
{
    public static BishFrame Compile(string code, BishScope? scope = null, bool excepts = false, bool optimize = true)
    {
        var stream = CharStreams.fromString(code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);

        if (excepts)
        {
            parser.RemoveErrorListeners();
            lexer.RemoveErrorListeners();
            parser.ErrorHandler = new ThrowOnErrorStrategy();
            lexer.AddErrorListener(new ThrowingLexerErrorListener());
        }

        var tree = parser.program();
        var visitor = new BishVisitor();
        var codes = visitor.VisitFull(tree, optimize: optimize);
        return new BishFrame(codes, scope);
    }
}

public class ThrowOnErrorStrategy : DefaultErrorStrategy
{
    public override void ReportError(Parser recognizer, RecognitionException e) =>
        throw new RecognitionException(e.Message, recognizer, e.InputStream, e.Context as ParserRuleContext);

    public override void Recover(Parser recognizer, RecognitionException e) => throw e;

    public override IToken RecoverInline(Parser recognizer) => throw new InputMismatchException(recognizer);

    public override void Sync(Parser recognizer)
    {
    }
}

public class ThrowingLexerErrorListener : IAntlrErrorListener<int>
{
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int column,
        string msg, RecognitionException e) =>
        throw new RecognitionException($"Syntax error at row {line} column {column}: {msg}", recognizer, null,
            e.Context as ParserRuleContext);
}