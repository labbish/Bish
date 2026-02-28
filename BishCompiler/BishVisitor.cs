global using Codes = System.Collections.Generic.List<BishBytecode.BishBytecode>;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using BishBytecode;
using BishBytecode.Bytecodes;
using BishRuntime;
using String = BishBytecode.Bytecodes.String;

namespace BishCompiler;

public partial class BishVisitor : BishBaseVisitor<Codes>
{
    public const string Anonymous = "anonymous";
    protected readonly SymbolAllocator Symbols = new();
    public List<CompilationError> Errors { get; } = [];

    private Codes Error(ParserRuleContext context, string message)
    {
        var start = context.Start;
        var stop = context.Stop;
        Errors.Add(new CompilationError(start.Line, start.Column, message,
            stop.Line, stop.Column + (stop.Text?.Length ?? 0)));
        return [];
    }

    private T? Try<T>(ParserRuleContext context, Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception e)
        {
            Error(context, e.Message);
            return default;
        }
    }

    private static BishBytecode.BishBytecode Tag(string tag) => new Nop().Tagged(tag);

    private static int ToInt(string text)
    {
        if (text[0] != '0' || text.Length < 2) return int.Parse(text);
        var radix = text[1] switch { 'x' => 16, 'o' => 8, 'b' => 2, _ => 10 };
        return Convert.ToInt32(text[2..], radix);
    }

    private static double ToNum(string text)
    {
        var pos = text.IndexOf('e');
        if (pos == -1) return double.Parse(text);
        var part = text[(pos + 1)..];
        var exp = "+-".Contains(part[0]) ? ToInt(part[1..]) * (part[0] == '-' ? -1 : 1) : ToInt(part);
        return double.Parse(text[..pos]) * Math.Pow(10, exp);
    }

    public override Codes VisitIntAtom(BishParser.IntAtomContext context) =>
        [new Int(ToInt(context.INT().GetText()))];

    public override Codes VisitNumAtom(BishParser.NumAtomContext context) =>
        [new Num(ToNum(context.NUM().GetText()))];

    public override Codes VisitStrAtom(BishParser.StrAtomContext context)
    {
        var str = context.STR().GetText();
        var raw = str.StartsWith('r');
        var text = str.TrimStart('r').Trim('#')[1..^1];
        return [new String(raw ? text : Regex.Unescape(text))];
    }

    public override Codes VisitNullAtom(BishParser.NullAtomContext context) => [new Null()];

    public override Codes VisitBoolAtom(BishParser.BoolAtomContext context) =>
        [new Bool(context.BOL().GetText() == "true")];

    public override Codes VisitIdAtom(BishParser.IdAtomContext context) => [new Get(context.GetText())];

    public override Codes VisitParenExpr(BishParser.ParenExprContext context) => Visit(context.expr());

    private static Op Op(string op, int argc) => new(BishOperator.GetOperatorName(op, argc), argc);

    public override Codes VisitUnOpExpr(BishParser.UnOpExprContext context) =>
        [..Visit(context.expr()), context.op.Text == "!" ? new Not() : Op(context.op.Text, 1)];

    public override Codes VisitBinOpExpr(BishParser.BinOpExprContext context)
    {
        return
        [
            ..Visit(context.left), ..Visit(context.right),
            ..(Codes)(context.op.Text switch
            {
                "===" => [new RefEq()],
                "!==" => [new RefEq(), new Not()],
                { } op => [Op(op, 2)]
            })
        ];
    }

    public override Codes VisitSingleIndex(BishParser.SingleIndexContext context) => Visit(context.expr());

    private Codes EvalOrNull(BishParser.ExprContext? context) => context is null ? [new Null()] : Visit(context);

    private Codes? VisitOrNull(ParserRuleContext? context) => context is null ? null : Visit(context);

    public override Codes VisitRangeIndex(BishParser.RangeIndexContext context) =>
    [
        new Get("range"),
        ..EvalOrNull(context.start),
        ..EvalOrNull(context.end),
        ..EvalOrNull(context.step),
        new Call(3)
    ];

    public override Codes VisitLogicAndExpr(BishParser.LogicAndExprContext context)
    {
        var tag = Symbols.Get("bin_and");
        return
        [
            ..Visit(context.left), new Op("bool", 1), new Copy(),
            new JumpIfNot(tag), new Pop(), ..Visit(context.right), Tag(tag)
        ];
    }

    public override Codes VisitLogicOrExpr(BishParser.LogicOrExprContext context)
    {
        var tag = Symbols.Get("bin_or");
        return
        [
            ..Visit(context.left), new Op("bool", 1), new Copy(),
            new JumpIf(tag), new Pop(), ..Visit(context.right), Tag(tag)
        ];
    }

    public override Codes VisitNullCombExpr(BishParser.NullCombExprContext context)
    {
        var tag = Symbols.Get("bin_or");
        return
        [
            ..Visit(context.left), new Copy(), new IsNull(),
            new JumpIfNot(tag), new Pop(), ..Visit(context.right), Tag(tag)
        ];
    }

    private Codes Condition(string name, Codes cond, Codes left, Codes right)
    {
        var (tag, end) = Symbols.GetPair(name);
        return Wrap([
            ..cond,
            new JumpIfNot(tag),
            ..left,
            new Jump(end),
            Tag(tag),
            ..right,
            Tag(end)
        ]);
    }

    public override Codes VisitTernOpExpr(BishParser.TernOpExprContext context) =>
        Condition("tern", Visit(context.cond), Visit(context.left), Visit(context.right));

    private Codes Call(BishParser.ArgContext[] args) => NoRest(args)
        ? [..args.SelectMany(Visit), new Call(args.Length)]
        : [..ToList(args), new CallArgs()];

    public override Codes VisitListExpr(BishParser.ListExprContext context)
    {
        var args = context.args().arg();
        return NoRest(args) ? [..args.SelectMany(Visit), new BuildList(args.Length)] : ToList(args);
    }

    protected Codes ToList(BishParser.ArgContext[] args) =>
    [
        new BuildList(0),
        ..args.SelectMany(arg => Visit(arg).Concat(arg switch
        {
            BishParser.RestArgContext => [new Op("op_add", 2)],
            _ => [new Swap(), new GetMember("add"), new Swap(), new Call(1)]
        }))
    ];

    protected static bool NoRest(BishParser.ArgContext[] args) => !args.Any(arg => arg is BishParser.RestArgContext);

    public override Codes VisitMapExpr(BishParser.MapExprContext context) =>
    [
        new Get("map"),
        new Call(0),
        ..context.entries().entry().SelectMany(entry => (Codes)(entry switch
        {
            BishParser.RestEntryContext rest => [..Visit(rest.expr()), Op("+", 2)],
            BishParser.SingleEntryContext single =>
                [new Copy(), ..Visit(single.key), ..Visit(single.value), Op("set[]", 3), new Pop()],
            _ => throw new ArgumentException("impossible")
        }))
    ];

    public override Codes VisitEmptyStat(BishParser.EmptyStatContext context) => [];

    public override Codes VisitExprStat(BishParser.ExprStatContext context) =>
        [..Visit(context.expr()), new EndStat()];

    public override Codes VisitBlockStat(BishParser.BlockStatContext context) =>
        Wrap(context.stat().SelectMany(Visit).ToList());

    private static Codes Wrap(params Codes[] codes) => [new Inner(), ..codes.SelectMany(x => x), new Outer()];

    public override Codes VisitProgram(BishParser.ProgramContext context) =>
        context.stat().SelectMany(Visit).ToList();

    internal abstract record Unbound(ParserRuleContext Context) : BishBytecode.BishBytecode
    {
        public abstract string ErrorMessage();
        public override void Execute(BishFrame frame) => throw new ArgumentException(ErrorMessage());
    }

    public Codes VisitFull(IParseTree tree, bool optimize)
    {
        var codes = Visit(tree);
        foreach (var code in codes)
            if (code is Unbound unbound)
                Error(unbound.Context, unbound.ErrorMessage());
        return optimize ? BishOptimizer.Optimize(codes) : codes;
    }
}

public class SymbolAllocator
{
    public readonly Dictionary<string, int> Used = [];

    public string Get(string symbol)
    {
        var used = Used.GetValueOrDefault(symbol);
        Used[symbol] = used + 1;
        return symbol + (used == 0 ? "" : $"_{used}");
    }

    public (string, string) GetPair(string symbol) => (Get(symbol), Get(symbol + "_end"));
}