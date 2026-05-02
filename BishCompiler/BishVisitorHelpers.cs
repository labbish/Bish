using System.Collections.Concurrent;
using BishUtils;

namespace BishCompiler;

public class SymbolAllocator
{
    public readonly IDictionary<string, int> Used = new ConcurrentDictionary<string, int>();

    public string Get(string symbol)
    {
        var used = Used.GetValueOrDefault(symbol);
        Used[symbol] = used + 1;
        return symbol + (used == 0 ? "" : $"_{used}");
    }

    public (string, string) GetPair(string symbol) => (Get(symbol), Get(symbol + "_end"));
}

public enum StackEffect
{
    Stat, // 0
    Expr, // +1
    Trans, // 0
    Pattern, // 0
    Consume // -1
}

public class CompileResult(
    StackEffect effect,
    IParseTree? tree,
    Codes? codes = null,
    IList<CompilationError>? errors = null)
{
    public StackEffect Effect => effect;
    public IParseTree? Tree => tree;
    public Codes Codes { get; internal set; } = (codes ?? []).ToConcurrentList();
    public readonly IList<CompilationError> Errors = (errors ?? []).ToConcurrentList();

    public CompileResult Error(IParseTree? parseTree, string message)
    {
        Errors.Add(new CompilationError(SourcePosition.From(parseTree), message));
        return this;
    }

    public CompileResult Error(string message) => Error(Tree, message);

    public T? Try<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception e)
        {
            Error(e.Message);
            return default;
        }
    }

    public static CompileResult Expr(IParseTree? tree) => new(StackEffect.Expr, tree);

    public static CompileResult Stat(IParseTree? tree) => new(StackEffect.Stat, tree);

    public static CompileResult Pattern(IParseTree? tree) => new(StackEffect.Pattern, tree);

    public static CompileResult Same(IParseTree? tree, params IList<CompileResult> results)
    {
        var effect = results[0].Effect;
        var result = new CompileResult(effect, tree);
        if (results.Any(e => e.Effect != effect))
            result.Error("Expect all branches to have a same effect: "
                         + $"found {string.Join(", ", results.Select(e => e.Effect))}");
        return result;
    }

    public CompileResult Wrap()
    {
        Codes = [new Inner(), ..Codes, new Outer()];
        return this;
    }

    public CompileResult Unwrap()
    {
        if (Codes.FirstOrDefault() is Inner && Codes.LastOrDefault() is Outer)
            Codes = Codes.Slice(1, -1);
        return this;
    }

    public CompileResult Add(BishBytecode code)
    {
        Codes.Add(code);
        return this;
    }

    public CompileResult TryAdd(Func<BishBytecode> func)
    {
        var code = Try(func);
        return code is null ? this : Add(code);
    }

    public CompileResult Add(params Codes code)
    {
        Codes.AddRange(code);
        return this;
    }

    public CompileResult Add(CompileResult result, StackEffect? expect = null)
    {
        Codes.AddRange(result.Codes);
        Errors.AddRange(result.Errors);
        if (expect is not null && expect != result.Effect)
            result.Error($"Expect {expect}, got {result.Effect}");
        return this;
    }

    public CompileResult Full(bool optimize)
    {
        foreach (var code in Codes)
            if (code is Unbound unbound)
                Error(unbound.Context, unbound.ErrorMessage());
        if (optimize) Codes = BishOptimizer.Optimize(Codes);
        return this;
    }

    public bool IsAsync()
    {
        var starts = 0;
        var ends = 0;
        foreach (var code in Codes)
        {
            switch (code)
            {
                case FuncStart:
                    starts++;
                    break;
                case FuncEnd:
                    ends++;
                    break;
                case Await when starts <= ends:
                    return true;
            }
        }

        return false;
    }
}

internal abstract record Unbound(ParserRuleContext Context) : BishBytecode
{
    public abstract string ErrorMessage();
    public override void Execute(BishFrame frame) => throw BishVisitor.Impossible;
}

public static class SourcePositionHelper
{
    extension(SourcePosition)
    {
        public static SourcePosition From(IToken token)
        {
            var line = token.Line;
            var column = token.Column;
    
            var text = token.Text;
            var lines = text.Split('\n');

            var stopLine = line + lines.Length - 1;
            var stopColumn = lines.Length > 1 ? lines.Last().Length : column + text.Length;

            return new SourcePosition(line, column, stopLine, stopColumn);
        }

        public static SourcePosition FromNode(ITerminalNode node) => SourcePosition.From(node.Symbol);

        public static SourcePosition FromCtx(ParserRuleContext ctx)
        {
            var start = ctx.Start;
            var stop = ctx.Stop;
            return new SourcePosition(start?.Line ?? 0, start?.Column ?? 0, 
                stop?.Line ?? 0, stop?.Column ?? 0 + (stop?.Text?.Length ?? 0));
        }

        public static SourcePosition From(IParseTree? tree) => tree switch
        {
            null => new SourcePosition(0, 0, 0, 0),
            ITerminalNode node => SourcePosition.FromNode(node),
            ParserRuleContext ctx => SourcePosition.FromCtx(ctx),
            _ => throw BishVisitor.Impossible
        };
    }
}