using BishBytecode;
using BishBytecode.Bytecodes;

namespace BishCompiler;

public partial class BishVisitor
{
    public override Codes VisitDefaultPattern(BishParser.DefaultPatternContext context) => [new Pop(), new Bool(true)];

    public override Codes VisitNullPattern(BishParser.NullPatternContext context) => [new IsNull()];

    public override Codes VisitParenPattern(BishParser.ParenPatternContext context) => Visit(context.pattern());

    public override Codes VisitExprPattern(BishParser.ExprPatternContext context) =>
        [..Visit(context.expr()), Op("==", 2)];

    public override Codes VisitOpPattern(BishParser.OpPatternContext context) =>
        [..Visit(context.expr()), Op(context.op.GetText(), 2)];

    public override Codes VisitTypePattern(BishParser.TypePatternContext context)
    {
        var name = context.ID()?.GetText();
        var tag = Symbols.Get("is_of");
        return
        [
            ..Visit(context.type),
            new TestType(tag),
            name is null ? new Nop() : new Def(name),
            new Pop().Tagged(tag)
        ];
    }

    public override Codes VisitNotPattern(BishParser.NotPatternContext context) =>
        [..Visit(context.pattern()), new Not()];

    public override Codes VisitAndPattern(BishParser.AndPatternContext context)
    {
        var (tag, end) = Symbols.GetPair("and");
        return
        [
            new Copy(),
            ..Visit(context.left),
            new Op("op_Bool", 1),
            new Copy(),
            new JumpIf(tag),
            new Swap(),
            new Pop(),
            new Jump(end),
            new Nop().Tagged(tag),
            new Pop(),
            ..Visit(context.right),
            new Nop().Tagged(end)
        ];
    }

    public override Codes VisitOrPattern(BishParser.OrPatternContext context)
    {
        var (tag, end) = Symbols.GetPair("or");
        return
        [
            new Copy(),
            ..Visit(context.left),
            new Op("op_Bool", 1),
            new Copy(),
            new JumpIfNot(tag),
            new Swap(),
            new Pop(),
            new Jump(end),
            new Nop().Tagged(tag),
            new Pop(),
            ..Visit(context.right),
            new Nop().Tagged(end)
        ];
    }

    public override Codes VisitMatchExpr(BishParser.MatchExprContext context) =>
        [..Visit(context.expr()), ..Visit(context.pattern())];

    public override Codes VisitSwitchExpr(BishParser.SwitchExprContext context) => Switch(1, Visit(context.expr()),
        context.caseExpr().Select(branch => (Visit(branch.pattern()), Wrap(Visit(branch.expr())))).ToList());

    public override Codes VisitSwitchStat(BishParser.SwitchStatContext context) => Switch(0, Visit(context.expr()),
        context.caseStat().Select(branch => (Visit(branch.pattern()), Wrap(Visit(branch.stat())))).ToList());

    private Codes Switch(int count, Codes expr, List<(Codes pattern, Codes codes)> branches) => Wrap([
        count == 0 ? new Nop() : new Null(), ..expr,
        ..branches.Reversed().Aggregate((Codes)(count == 0 ? [] : [new Null()]),
            (current, branch) => Condition("case",
                [new Copy(), ..branch.pattern], branch.codes, current)),
        new Swap(count * 2), new Pop(count + 1)
    ]);
}