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

    private static CompilerResult<BishParseTree> Parse(ICodeSource source)
    {
        var stream = CharStreams.fromString(source.Code);
        var lexer = new BishLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new BishParser(tokens);

        var listener = new ErrorListener();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var file = source.Filename;
        SetFile(listener.Errors, file);
        return new CompilerResult<BishParseTree>(BishParseTree.From(parser.program()).WithFile(file), listener.Errors);
    }

    private static CompilerResult<Codes> Compile(CompilerResult<BishParseTree> compilerResult, CompileOptions options)
    {
        var (tree, errors) = compilerResult;
        var result = new BishVisitor().VisitFull(tree, optimize: options.Optimize);
        SetFile(result.Errors, tree.File);
        if (options.Throws) BishCompileService.CheckErrors(result.Errors);
        return new CompilerResult<Codes>(result.Codes, errors.Concat(result.Errors).ToConcurrentList());
    }

    private static void SetFile(IList<CompilationError> errors, string? file)
    {
        foreach (var error in errors) error.File ??= file;
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