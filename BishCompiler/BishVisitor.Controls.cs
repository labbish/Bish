namespace BishCompiler;

public partial class BishVisitor
{
    public override CompileResult VisitIfExpr(BishParser.IfExprContext context) =>
        Condition("if", Visit(context.cond), Visit(context.left).Wrap(),
            context.right is null ? null : Visit(context.right).Wrap());

    internal record LoopUnbound(ParserRuleContext Context, string Name, string? LoopTag) : Unbound(Context)
    {
        public int Depth;

        public LoopUnbound Deeper()
        {
            Depth++;
            return this;
        }

        public override string ErrorMessage() => $"Found {Name} statement out of loop!";
    }

    public override CompileResult VisitBreakExpr(BishParser.BreakExprContext context) =>
        CompileResult.Expr(context).Add(new Break(context, context.ID()?.GetText()));

    internal record Break(ParserRuleContext Context, string? LoopTag) : LoopUnbound(Context, "break", LoopTag);

    public override CompileResult VisitContinueExpr(BishParser.ContinueExprContext context) =>
        CompileResult.Expr(context).Add(new Continue(context, context.ID()?.GetText()));

    internal record Continue(ParserRuleContext Context, string? LoopTag) : LoopUnbound(Context, "continue", LoopTag);

    public override CompileResult VisitWhileExpr(BishParser.WhileExprContext context)
    {
        var (tag, end) = Symbols.GetPair("while");
        return CompileResult.Expr(context)
            .Add(Tag(tag))
            .Add(Visit(context.cond), StackEffect.Expr)
            .Add(new JumpIfNot(end))
            .Add(Visit(context.loop).IntoStat().Wrap())
            .Add(new Jump(tag))
            .Add(Tag(end))
            .WrapLoop(end, tag, context.tag()?.ID().GetText())
            .Add(new Null());
    }

    public override CompileResult VisitDoWhileExpr(BishParser.DoWhileExprContext context)
    {
        var (tag, end) = Symbols.GetPair("do_while");
        var @continue = Symbols.Get("do_while_continue");
        return CompileResult.Expr(context)
            .Add(Tag(tag))
            .Add(Visit(context.loop).IntoStat().Wrap(), StackEffect.Stat)
            .Add(Tag(@continue))
            .Add(Visit(context.cond), StackEffect.Expr)
            .Add(new JumpIf(tag), Tag(end))
            .WrapLoop(end, @continue, context.tag()?.ID().GetText())
            .Add(new Null());
    }

    public override CompileResult VisitForExpr(BishParser.ForExprContext context) =>
        CompileResult.Expr(context)
            .Add(Visit(context.forBody().iter), StackEffect.Consume)
            .Add(ForIter(context, new CompileResult(StackEffect.Trans, context).Add(new Move("$for"))
                    .Add(Def(context.forBody().obj, CompileResult.Expr(null).Add(new Del("$for"))))
                    .Add(new Pop())
                    .Add(Visit(context.loop).IntoStat()), context.tag()?.ID().GetText(),
                context.forBody().AWT() is not null));

    private CompileResult ForIter(ParserRuleContext context, CompileResult body, string? loopTag, bool await = false)
    {
        var (tag, end) = Symbols.GetPair("for_iter");
        var @break = Symbols.Get("for_iter_break");
        var result = new CompileResult(StackEffect.Trans, context)
            .Add(Op("iter", 1), new Copy().Tagged(tag))
            .Add(new GetMember("next"), new Call(0));
        if (await) result.Add(new Await());
        return result.Add(new Copy(), new GetBuiltin("IteratorStop"), new RefEq())
            .Add(new JumpIf(end))
            .Add(new Inner())
            .Add(body, StackEffect.Consume)
            .Add(new Outer())
            .Add(new Jump(tag), new Pop().Tagged(end), new Pop().Tagged(@break))
            .WrapLoop(@break, tag, loopTag, true)
            .Add(new Null());
    }
}