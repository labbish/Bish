using BishBytecode;
using BishBytecode.Bytecodes;

namespace BishCompiler;

public partial class BishVisitor
{
    public override Codes VisitNullPattern(BishParser.NullPatternContext context) => [new IsNull()];

    public override Codes VisitParenPattern(BishParser.ParenPatternContext context) => Visit(context.pattern());

    public override Codes VisitListPattern(BishParser.ListPatternContext context)
    {
        var items = context.item();
        var pos = -1;
        for (var i = 0; i < items.Length; i++)
            if (items[i].dots is not null)
            {
                if (pos == -1) pos = i;
                else return Error(context, "Found list deconstruct pattern with multiple rest pattern");
            }

        var end = Symbols.Get("list");
        var tags = Enumerable.Range(0, items.Length).Select(_ => Symbols.Get("list")).ToList();
        return
        [
            new ListDeconstruct(items.Length, pos, Pattern: true),
            new JumpIfNot(tags[^1]),
            ..items.SelectMany((item, i) => Visit(item.pattern()).Concat([new JumpIfNot(tags[i])])),
            new Bool(true),
            new Jump(end),
            ..tags[..^1].Select(t => new Pop().Tagged(t)),
            Tag(tags[^1]),
            new Bool(false),
            Tag(end)
        ];
    }

    public override Codes VisitExprPattern(BishParser.ExprPatternContext context) =>
        context.expr().GetText() == "_" ? [new Pop(), new Bool(true)] : [..Visit(context.expr()), Op("==", 2)];

    public override Codes VisitOpPattern(BishParser.OpPatternContext context) =>
        [..Visit(context.expr()), Op(context.op.GetText(), 2)];

    public override Codes VisitTypePattern(BishParser.TypePatternContext context)
    {
        var name = context.ID()?.GetText();
        var tag = Symbols.Get("is_of");
        return
        [
            ..context.type.GetText() == "_" ? [new Get("object")] : Visit(context.type),
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
            Op("bool", 1),
            new Copy(),
            new JumpIf(tag),
            new Swap(),
            new Pop(),
            new Jump(end),
            Tag(tag),
            new Pop(),
            ..Visit(context.right),
            Tag(end)
        ];
    }

    public override Codes VisitOrPattern(BishParser.OrPatternContext context)
    {
        var (tag, end) = Symbols.GetPair("or");
        return
        [
            new Copy(),
            ..Visit(context.left),
            Op("bool", 1),
            new Copy(),
            new JumpIfNot(tag),
            new Swap(),
            new Pop(),
            new Jump(end),
            Tag(tag),
            new Pop(),
            ..Visit(context.right),
            Tag(end)
        ];
    }

    public override Codes VisitWhenPattern(BishParser.WhenPatternContext context)
    {
        var tag = Symbols.Get("when");
        return
        [
            ..Visit(context.pattern()), Op("bool", 1), new Copy(),
            new JumpIfNot(tag), new Pop(), ..Visit(context.expr()), Tag(tag)
        ];
    }

    public override Codes VisitMatchExpr(BishParser.MatchExprContext context) =>
        [..Visit(context.expr()), ..Visit(context.pattern())];

    public override Codes VisitAsExpr(BishParser.AsExprContext context) =>
    [
        ..Visit(context.obj),
        ..Visit(context.type),
        new TestType(),
        new Swap(),
        new Pop()
    ];

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

    public override Codes VisitPipeVarExpr(BishParser.PipeVarExprContext context) => [new Get("$")];

    public override Codes VisitPipe(BishParser.PipeContext context) => Visit(context.expr());

    public override Codes VisitPipeExpr(BishParser.PipeExprContext context)
    {
        var tag = Symbols.Get("tag");
        return Wrap([
            ..Visit(context.expr()),
            ..context.pipe().SelectMany(pipe => (Codes)(pipe.op is null
                ? [new Move("$"), ..Visit(pipe)]
                : [new Copy(), new IsNull(), new JumpIf(tag), new Move("$"), ..Visit(pipe)])),
            Tag(tag)
        ]);
    }

    public override Codes VisitTryCallExpr(BishParser.TryCallExprContext context) =>
        [..Visit(context.expr()), new TryFunc(), ..Call(context.args().arg())];
}