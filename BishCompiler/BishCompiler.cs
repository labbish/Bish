global using Codes = System.Collections.Generic.IList<BishRuntime.BishBytecode>;
global using Antlr4.Runtime.Tree;
global using Antlr4.Runtime;
global using BishRuntime;
using System.Runtime.CompilerServices;
using BishUtils;

namespace BishCompiler;

public static class BishCompiler
{
    private static CompilerResult<BishObject> Parse(string code)
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

        return new CompilerResult<BishObject>(BishParseTreeObject.From(parser.program()), listener.Errors);
    }

    private static CompilerResult<Codes> Compile(CompilerResult<BishObject> compilerResult, CompileOptions options)
    {
        var (obj, errors) = compilerResult;
        var tree = obj.As<BishParseTreeObject>("parse tree");
        var result = new BishVisitor().VisitFull(tree.Tree, optimize: options.Optimize);
        if (options.Throws) BishCompileService.CheckErrors(result.Errors);
        return new CompilerResult<Codes>(result.Codes, errors.Concat(result.Errors).ToConcurrentList());
    }

    public static void Init() => RuntimeHelpers.RunClassConstructor(typeof(BishCompiler).TypeHandle);

    static BishCompiler()
    {
        BishBuiltinBinder.Init();
        BishLib.BishLib.Initialize();
        BishCompileService.Parser = Parse;
        BishCompileService.Compiler = Compile;
        try
        {
            BishImporter.Import(null, "preludes");
        }
        catch (BishException e)
        {
            Console.Error.WriteLine($"Cannot import preludes: {e}");
        }
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