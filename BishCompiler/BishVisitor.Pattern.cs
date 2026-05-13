using BishUtils;

namespace BishCompiler;

public partial class BishVisitor
{
    public override CompileResult VisitNullPattern(BishParser.NullPatternContext context) =>
        CompileResult.Pattern(context).Add(new GetBuiltin("null")).Add(new RefEq());

    public override CompileResult VisitParenPattern(BishParser.ParenPatternContext context) => Visit(context.pattern());

    public override CompileResult VisitListPattern(BishParser.ListPatternContext context)
    {
        var result = CompileResult.Pattern(context);
        var items = context.patItem();
        var pos = -1;
        foreach (var (item, i) in items.Enumerate())
        {
            if (item.dots is null) continue;
            if (pos == -1) pos = i;
            else result.Error("Found list deconstruct pattern with multiple rest pattern");
        }

        var end = Symbols.Get("list");
        var tags = Enumerable.Range(0, items.Length).Select(_ => Symbols.Get("list")).ToList();
        result.Add(new ListDeconstruct(items.Length, pos, Pattern: true), new JumpIfNot(tags[^1]));
        foreach (var (item, i) in items.Enumerate())
            result.Add(Visit(item.pattern()), StackEffect.Pattern).Add(new JumpIfNot(tags[i]));
        result.Add(new Bool(true), new Jump(end));
        foreach (var tag in tags[..^1]) result.Add(new Pop().Tagged(tag));
        result.Add(Tag(tags[^1]), new Bool(false), Tag(end));
        return result;
    }

    public override CompileResult VisitMapPattern(BishParser.MapPatternContext context)
    {
        var result = CompileResult.Pattern(context);
        var entries = context.patEntry();
        if (entries.SkipLast(1).Any(entry => entry is BishParser.RestPatternEntryContext))
            result.Error(context, "Rest entry must be the last one in map deconstruction");
        var (tag, end) = Symbols.GetPair("map");
        result.Add(new Copy(), new GetBuiltin("map"), new TestType(), new Pop(),
            new JumpIfNot(tag), new GetBuiltin("map"), new Swap(), new Call(1));
        foreach (var entry in entries)
            switch (entry)
            {
                case BishParser.SinglePatternEntryContext single:
                    result.Add(new Copy())
                        .Add(Visit(single.expr()), StackEffect.Expr)
                        .Add(new TryDelIndex(), new JumpIfNot(tag))
                        .Add(Visit(single.pattern()), StackEffect.Pattern)
                        .Add(new JumpIfNot(tag));
                    break;
                case BishParser.RestPatternEntryContext rest:
                    result.Add(new Copy()).Add(Visit(rest.pattern()), StackEffect.Pattern).Add(new JumpIfNot(tag));
                    break;
                default: throw Impossible;
            }

        result.Add(new Bool(true), new Jump(end), Tag(tag), new Bool(false), Tag(end), new Swap(), new Pop());
        return result;
    }

    public override CompileResult VisitObjPattern(BishParser.ObjPatternContext context)
    {
        var result = CompileResult.Pattern(context);
        var (tag, end) = Symbols.GetPair("map");
        foreach (var entry in context.patObjEntry())
            result.Add(new Copy(), new TryGetMember(entry.ID().GetText()), new JumpIfNot(tag))
                .Add(Visit(entry.pattern()), StackEffect.Pattern).Add(new JumpIfNot(tag));
        return result.Add(new Bool(true), new Jump(end), Tag(tag), new Bool(false), Tag(end), new Swap(), new Pop());
    }

    public override CompileResult VisitExprPattern(BishParser.ExprPatternContext context) =>
        context.expr().GetText() == "_"
            ? CompileResult.Pattern(context).Add(new Pop(), new Bool(true))
            : CompileResult.Pattern(context).Add(Visit(context.expr()), StackEffect.Expr).Add(Op("==", 2));

    public override CompileResult VisitOpPattern(BishParser.OpPatternContext context) =>
        CompileResult.Pattern(context)
            .Add(Visit(context.expr()), StackEffect.Expr).Add(Op(context.op.GetText(), 2));

    private CompileResult TestType(ParserRuleContext? context, string tagName, CompileResult type,
        object? var, CompileResult? post = null)
    {
        var result = CompileResult.Pattern(context);
        var tag = Symbols.Get(tagName);
        result.Add(type, StackEffect.Expr).Add(new TestType(tag));
        // ReSharper disable once InvertIf
        if (var is not null)
        {
            var value = CompileResult.Expr(null).Add(new Del("$_"));
            if (post is not null) result.Add(post, StackEffect.Trans);
            result.Add(new Move("$_")).Add(var switch
            {
                BishParser.ExprContext expr => Def(expr, value),
                string id => Def(id, value),
                _ => throw Impossible
            });
        }

        return result.Add(new Pop().Tagged(tag));
    }

    public override CompileResult VisitTypePattern(BishParser.TypePatternContext context) =>
        TestType(context, "is_of",
            context.type.GetText() == "_"
                ? CompileResult.Expr(null).Add(new GetBuiltin("object"))
                : Visit(context.type), context.var);

    private CompileResult IsErr(ParserRuleContext context, object? var) =>
        TestType(context, "is_err", CompileResult.Expr(null).Add(new GetBuiltin("Error$Result")), var,
            new CompileResult(StackEffect.Trans, null).Add(new GetMember("error")));

    public override CompileResult VisitErrPattern(BishParser.ErrPatternContext context) =>
        IsErr(context, context.expr());

    public override CompileResult VisitNotPattern(BishParser.NotPatternContext context) =>
        CompileResult.Pattern(context).Add(Visit(context.pattern()), StackEffect.Pattern).Add(new Not());

    public override CompileResult VisitAndPattern(BishParser.AndPatternContext context)
    {
        var (tag, end) = Symbols.GetPair("and");
        return CompileResult.Pattern(context)
            .Add(new Copy())
            .Add(Visit(context.left), StackEffect.Pattern)
            .Add(Op("bool", 1), new Copy(), new JumpIf(tag), new Swap(),
                new Pop(), new Jump(end), Tag(tag), new Pop())
            .Add(Visit(context.right), StackEffect.Pattern)
            .Add(Tag(end));
    }

    public override CompileResult VisitOrPattern(BishParser.OrPatternContext context)
    {
        var (tag, end) = Symbols.GetPair("or");
        return CompileResult.Pattern(context)
            .Add(new Copy())
            .Add(Visit(context.left), StackEffect.Pattern)
            .Add(Op("bool", 1), new Copy(), new JumpIfNot(tag), new Swap(),
                new Pop(), new Jump(end), Tag(tag), new Pop())
            .Add(Visit(context.right), StackEffect.Pattern)
            .Add(Tag(end));
    }

    public override CompileResult VisitWhenPattern(BishParser.WhenPatternContext context)
    {
        var tag = Symbols.Get("when");
        return CompileResult.Pattern(context)
            .Add(Visit(context.pattern()), StackEffect.Pattern)
            .Add(Op("bool", 1), new Copy(), new JumpIfNot(tag), new Pop())
            .Add(Visit(context.expr()), StackEffect.Expr)
            .Add(Tag(tag));
    }

    public override CompileResult VisitMatchExpr(BishParser.MatchExprContext context) =>
        CompileResult.Expr(context).Add(Visit(context.expr()), StackEffect.Expr)
            .Add(Visit(context.pattern()), StackEffect.Pattern);

    public override CompileResult VisitAsExpr(BishParser.AsExprContext context) =>
        CompileResult.Expr(context).Add(Visit(context.obj), StackEffect.Expr)
            .Add(Visit(context.type), StackEffect.Expr).Add(new TestType(), new Swap(), new Pop());

    public override CompileResult VisitSwitchExpr(BishParser.SwitchExprContext context) =>
        Switch(context, Visit(context.expr()),
            context.caseExpr().Select(branch => (Visit(branch.pattern()), Visit(branch.expr()).Wrap())).ToList());

    private CompileResult Switch(ParserRuleContext context, CompileResult expr,
        IList<(CompileResult pattern, CompileResult codes)> branches)
    {
        var result = CompileResult.Same(context, branches.Select(pair => pair.codes).ToList());
        var count = result.Effect switch
        {
            StackEffect.Stat => 0,
            StackEffect.Expr => 1,
            _ => throw Impossible
        };
        if (count != 0) result.Add(new Null());
        return result.Add(expr, StackEffect.Expr)
            .Add(branches.Reverse().Aggregate(
                count == 0 ? CompileResult.Stat(null) : CompileResult.Expr(null).Add(new Null()),
                (current, branch) => Condition("case",
                    CompileResult.Expr(null).Add(new Copy()).Add(branch.pattern, StackEffect.Pattern), branch.codes,
                    current)))
            .Add(new Swap(count * 2), new Pop(count + 1));
    }

    public override CompileResult VisitPipeVarExpr(BishParser.PipeVarExprContext context) =>
        CompileResult.Expr(context).Add(new Get("$"));

    public override CompileResult VisitPipe(BishParser.PipeContext context) => Visit(context.expr());

    public override CompileResult VisitPipeExpr(BishParser.PipeExprContext context)
    {
        var result = new CompileResult(StackEffect.Expr, context);
        var tag = Symbols.Get("tag");
        result.Add(Visit(context.expr()), StackEffect.Expr);
        foreach (var pipe in context.pipe())
        {
            if (pipe.op is not null) result.Add(new Copy(), Op("nullish", 1), new JumpIf(tag));
            result.Add(new Move("$")).Add(Visit(pipe), StackEffect.Expr);
        }

        return result.Add(Tag(tag)).Wrap();
    }
}