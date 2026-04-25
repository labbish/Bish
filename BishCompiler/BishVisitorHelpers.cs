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
    Pattern // 0
}

public class CompileResult(
    StackEffect effect,
    ParserRuleContext? context,
    Codes? codes = null,
    IList<CompilationError>? errors = null)
{
    public StackEffect Effect => effect;
    public ParserRuleContext? Context => context;
    public Codes Codes { get; internal set; } = (codes ?? []).ToConcurrentList();
    public readonly IList<CompilationError> Errors = (errors ?? []).ToConcurrentList();

    public CompileResult Error(ParserRuleContext? ctx, string message)
    {
        var start = ctx?.Start;
        var stop = ctx?.Stop;
        Errors.Add(new CompilationError(start?.Line ?? 0, start?.Column ?? 0, message,
            stop?.Line ?? 0, stop?.Column ?? 0 + (stop?.Text?.Length ?? 0)));
        return this;
    }

    public CompileResult Error(string message) => Error(Context, message);

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

    public static CompileResult Expr(ParserRuleContext? context) => new(StackEffect.Expr, context);

    public static CompileResult Stat(ParserRuleContext? context) => new(StackEffect.Stat, context);

    public static CompileResult Pattern(ParserRuleContext? context) => new(StackEffect.Pattern, context);

    public static CompileResult Same(ParserRuleContext? context, params IList<CompileResult> results)
    {
        var effect = results[0].Effect;
        var result = new CompileResult(effect, context);
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
}

internal abstract record Unbound(ParserRuleContext Context) : BishBytecode
{
    public abstract string ErrorMessage();
    public override void Execute(BishFrame frame) => throw new ArgumentException(ErrorMessage());
}