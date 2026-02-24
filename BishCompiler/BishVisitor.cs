global using Codes = System.Collections.Generic.List<BishBytecode.BishBytecode>;
using System.Text.RegularExpressions;
using BishBytecode.Bytecodes;
using BishRuntime;
using String = BishBytecode.Bytecodes.String;

namespace BishCompiler;

public class BishVisitor : BishBaseVisitor<Codes>
{
    protected SymbolAllocator Allocator = new();

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
        var tag = Allocator.Symbol("and");
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
        var tag = Allocator.Symbol("or");
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

    public override Codes VisitExprStat(BishParser.ExprStatContext context) =>
        [..Visit(context.expr()), new Pop()]; // pops the expr result to make stack empty

    public override Codes VisitBlockStat(BishParser.BlockStatContext context) =>
        [new Inner(), ..context.stat().SelectMany(Visit), new Outer()];

    public override Codes VisitProgram(BishParser.ProgramContext context) =>
        context.stat().SelectMany(Visit).ToList();

    // TODO: support calling with rest args
    public override Codes VisitCallExpr(BishParser.CallExprContext context)
    {
        var args = context.args().expr();
        return [..args.SelectMany(Visit), ..Visit(context.func), new Call(args.Length)];
    }
}

public class SymbolAllocator
{
    public readonly Dictionary<string, int> Used = [];

    public string Symbol(string symbol)
    {
        var used = Used.GetValueOrDefault(symbol);
        Used[symbol] = used + 1;
        return symbol + (used == 0 ? "" : $"_{used}");
    }
}