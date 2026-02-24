global using Codes = System.Collections.Generic.List<BishBytecode.BishBytecode>;
using System.Text.RegularExpressions;
using BishBytecode.Bytecodes;
using BishRuntime;
using String = BishBytecode.Bytecodes.String;

namespace BishCompiler;

public class BishVisitor : BishBaseVisitor<Codes>
{
    protected readonly SymbolAllocator Symbols = new();

    public override Codes VisitIntAtom(BishParser.IntAtomContext context) =>
        [new Int(int.Parse(context.INT().GetText()))];

    public override Codes VisitNumAtom(BishParser.NumAtomContext context) =>
        [new Num(double.Parse(context.NUM().GetText()))];

    public override Codes VisitStrAtom(BishParser.StrAtomContext context) =>
        [new String(Regex.Unescape(context.STR().GetText()[1..^1]))];

    public override Codes VisitIdAtom(BishParser.IdAtomContext context) => [new Get(context.GetText())];

    public override Codes VisitParenExpr(BishParser.ParenExprContext context) => Visit(context.expr());

    public override Codes VisitBinOpExpr(BishParser.BinOpExprContext context) =>
        [..Visit(context.left), ..Visit(context.right), new Op(BishOperator.GetOperatorName(context.op.Text, 2), 2)];

    public override Codes VisitUnOpExpr(BishParser.UnOpExprContext context) =>
        [..Visit(context.expr()), new Op(BishOperator.GetOperatorName(context.op.Text, 1), 1)];

    public override Codes VisitGetMember(BishParser.GetMemberContext context) =>
        [..Visit(context.expr()), new GetMember(context.name.Text)];

    public override Codes VisitSet(BishParser.SetContext context) =>
        [..Visit(context.expr()), new Set(context.name.Text)];

    public override Codes VisitSetMember(BishParser.SetMemberContext context) =>
        [..Visit(context.value), ..Visit(context.obj), new SetMember(context.name.Text)];

    public override Codes VisitDef(BishParser.DefContext context) =>
        [..Visit(context.expr()), new Def(context.name.Text)];

    public override Codes VisitLogicAndExpr(BishParser.LogicAndExprContext context)
    {
        var tag = Symbols.Get("and");
        return
        [
            ..Visit(context.left),
            new Op("op_Bool", 1),
            new Copy(),
            new JumpIfNot(tag),
            new Pop(),
            ..Visit(context.right),
            new Nop().Tagged(tag)
        ];
    }

    public override Codes VisitLogicOrExpr(BishParser.LogicOrExprContext context)
    {
        var tag = Symbols.Get("or");
        return
        [
            ..Visit(context.left),
            new Op("op_Bool", 1),
            new Copy(),
            new JumpIf(tag),
            new Pop(),
            ..Visit(context.right),
            new Nop().Tagged(tag)
        ];
    }

    private Codes Condition(string name, Codes cond, Codes left, Codes right)
    {
        var (tag, end) = Symbols.GetPair(name);
        return
        [
            ..cond,
            new Op("op_Bool", 1),
            new JumpIfNot(tag),
            ..left,
            new Jump(end),
            new Nop().Tagged(tag),
            ..right,
            new Nop().Tagged(end)
        ];
    }

    public override Codes VisitTernOpExpr(BishParser.TernOpExprContext context) =>
        Condition("tern", Visit(context.cond), Visit(context.left), Visit(context.right));

    public override Codes VisitCallExpr(BishParser.CallExprContext context)
    {
        var args = context.args().arg();
        if (NoRest(args)) return [..args.SelectMany(Visit), ..Visit(context.func), new Call(args.Length)];
        return [..ToList(args), ..Visit(context.func), new CallArgs()];
    }

    public override Codes VisitListExpr(BishParser.ListExprContext context)
    {
        var args = context.args().arg();
        return NoRest(args) ? [..args.SelectMany(Visit), new BuildList(args.Length)] : ToList(args);
    }

    protected Codes ToList(BishParser.ArgContext[] args) =>
    [
        new BuildList(0),
        ..args.SelectMany<BishParser.ArgContext, BishBytecode.BishBytecode>(arg => arg switch
        {
            BishParser.RestArgContext => [..Visit(arg), new Op("op_Add", 2)],
            _ => [..Visit(arg), new Swap(), new GetMember("add"), new Call(1)]
        })
    ];

    protected static bool NoRest(BishParser.ArgContext[] args) => !args.Any(arg => arg is BishParser.RestArgContext);

    public override Codes VisitExprStat(BishParser.ExprStatContext context) =>
        [..Visit(context.expr()), new Pop()];

    public override Codes VisitBlockStat(BishParser.BlockStatContext context) =>
        Wrap(context.stat().SelectMany(Visit).ToList());

    private static Codes Wrap(params Codes[] codes) => [new Inner(), ..codes.SelectMany(x => x), new Outer()];

    public override Codes VisitIfStat(BishParser.IfStatContext context) =>
        Condition("if", Visit(context.cond), Wrap(Visit(context.left)),
            context.right is null ? [] : Wrap(Visit(context.right)));

    public override Codes VisitWhileStat(BishParser.WhileStatContext context)
    {
        var (tag, end) = Symbols.GetPair("while");
        return
        [
            new Nop().Tagged(tag),
            ..Visit(context.expr()),
            new JumpIfNot(end),
            ..Wrap(Visit(context.stat())),
            new Jump(tag),
            new Nop().Tagged(end)
        ];
    }

    public override Codes VisitDoWhileStat(BishParser.DoWhileStatContext context)
    {
        var tag = Symbols.Get("do_while");
        return
        [
            new Nop().Tagged(tag),
            ..Wrap(Visit(context.stat())),
            ..Visit(context.expr()),
            new JumpIf(tag)
        ];
    }

    public override Codes VisitForStat(BishParser.ForStatContext context)
    {
        var (tag, end) = Symbols.GetPair("for");
        return
        [
            ..Visit(context.init),
            new Nop().Tagged(tag),
            ..Visit(context.cond),
            new JumpIfNot(end),
            ..Wrap(Visit(context.stat()), Visit(context.step)),
            new Pop(),
            new Jump(tag),
            new Pop().Tagged(end)
        ];
    }

    public override Codes VisitForIterStat(BishParser.ForIterStatContext context)
    {
        var (tag, end) = Symbols.GetPair("for_iter");
        return [
            ..Visit(context.expr()),
            new Op("op_Iter", 1),
            new ForIter(end).Tagged(tag),
            ..Wrap([new Def(context.ID().GetText())], Visit(context.stat())),
            new Pop(),
            new Jump(tag),
            new Pop().Tagged(end)
        ];
    }

    public override Codes VisitProgram(BishParser.ProgramContext context) =>
        context.stat().SelectMany(Visit).ToList();
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