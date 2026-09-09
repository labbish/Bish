global using Codes = System.Collections.Generic.IList<BishRuntime.BishBytecode>;
global using Antlr4.Runtime.Tree;
global using Antlr4.Runtime;
global using BishRuntime;
using BishUtils;

namespace BishLanguage;

public struct BishLanguage : ILanguage
{
    public static string Name => "bish";
    public static BishRuntime.BishLanguage Language => new(Parse, Compile);

    private static CompilerResult<BishParseTree> Parse(string code)
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

        return new CompilerResult<BishParseTree>(BishParseTree.From(parser.program()), listener.Errors);
    }

    private static CompilerResult<Codes> Compile(CompilerResult<BishParseTree> compilerResult, CompileOptions options)
    {
        var (tree, errors) = compilerResult;
        var result = new BishVisitor().VisitFull(tree, optimize: options.Optimize);
        if (options.Throws) BishCompileService.CheckErrors(result.Errors);
        return new CompilerResult<Codes>(result.Codes, errors.Concat(result.Errors).ToConcurrentList());
    }
}

public class ErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    public IList<CompilationError> Errors { get; } = new ConcurrentList<CompilationError>();

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken token,
        int line, int pos, string msg, RecognitionException e) =>
        Errors.Add(new CompilationError(SourcePosition.From(token), msg));

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int token,
        int line, int pos, string msg, RecognitionException e) =>
        Errors.Add(new CompilationError(new SourcePosition(line, pos, line, pos + 1), msg));
}