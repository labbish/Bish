using Antlr4.Runtime;
using BishBytecode.Bytecodes;

namespace BishCompiler;

public static class LoopWrapper
{
    extension(CompileResult result)
    {
        internal CompileResult WrapLoop(string @break, string @continue, string? loopTag, bool pops = false)
        {
            result.Wrap();
            result.Codes = result.Codes.SelectMany(code => code switch
            {
                BishVisitor.Break x when MatchLoopTag(x.LoopTag, loopTag) =>
                    [..Enumerable.Repeat(new Pop(), x.Depth), new Jump(@break)],
                BishVisitor.Continue x when MatchLoopTag(x.LoopTag, loopTag) =>
                    [..Enumerable.Repeat(new Pop(), x.Depth), new Jump(@continue)],
                BishVisitor.LoopUnbound x when pops => [x.Deeper()],
                _ => (Codes)([code])
            }).ToList();
            return result;
        }
    }

    private static bool MatchLoopTag(string? unbound, string? loop) => unbound is null || unbound == loop;
}

public partial class BishVisitor
{
    public override CompileResult VisitIfStat(BishParser.IfStatContext context) =>
        Condition("if", Visit(context.cond), Visit(context.left).Wrap(),
            context.right is null ? CompileResult.Stat(context) : Visit(context.right).Wrap());

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

    public override CompileResult VisitBreakStat(BishParser.BreakStatContext context) =>
        CompileResult.Stat(context).Add(new Break(context, context.ID()?.GetText()));

    internal record Break(ParserRuleContext Context, string? LoopTag) : LoopUnbound(Context, "break", LoopTag);

    public override CompileResult VisitContinueStat(BishParser.ContinueStatContext context) =>
        CompileResult.Stat(context).Add(new Continue(context, context.ID()?.GetText()));

    internal record Continue(ParserRuleContext Context, string? LoopTag) : LoopUnbound(Context, "continue", LoopTag);

    public override CompileResult VisitWhileStat(BishParser.WhileStatContext context)
    {
        var (tag, end) = Symbols.GetPair("while");
        return CompileResult.Stat(context)
            .Add(Tag(tag))
            .Add(Visit(context.expr()), StackEffect.Expr)
            .Add(new JumpIfNot(end))
            .Add(Visit(context.stat()).Wrap(), StackEffect.Stat)
            .Add(new Jump(tag))
            .Add(Tag(end))
            .WrapLoop(end, tag, context.tag()?.ID().GetText());
    }

    public override CompileResult VisitDoWhileStat(BishParser.DoWhileStatContext context)
    {
        var (tag, end) = Symbols.GetPair("do_while");
        var @continue = Symbols.Get("do_while_continue");
        return CompileResult.Stat(context)
            .Add(Tag(tag))
            .Add(Visit(context.stat()).Wrap(), StackEffect.Stat)
            .Add(Tag(@continue))
            .Add(Visit(context.expr()), StackEffect.Expr)
            .Add(new JumpIf(tag), Tag(end))
            .WrapLoop(end, @continue, context.tag()?.ID().GetText());
    }

    public override CompileResult VisitForStat(BishParser.ForStatContext context)
    {
        var (tag, end) = Symbols.GetPair("for");
        var @continue = Symbols.Get("for_continue");
        return CompileResult.Stat(context)
            .Add(Visit(context.forStats().init), StackEffect.Expr)
            .Add(Tag(tag))
            .Add(Visit(context.forStats().cond), StackEffect.Expr)
            .Add(new JumpIfNot(end), new Inner())
            .Add(Visit(context.stat()), StackEffect.Stat)
            .Add(Tag(@continue))
            .Add(Visit(context.forStats().step), StackEffect.Expr)
            .Add(new Outer(), new Pop(), new Jump(tag), new Pop().Tagged(end))
            .WrapLoop(end, @continue, context.tag()?.ID().GetText(), true);
    }

    public override CompileResult VisitForIterStat(BishParser.ForIterStatContext context)
    {
        var (tag, end) = Symbols.GetPair("for_iter");
        return CompileResult.Stat(context)
            .Add(Visit(context.iter), StackEffect.Expr)
            .Add(Op("iter", 1), new ForIter(end).Tagged(tag))
            .Add(new Inner(), new Move("$for"))
            .Add(context.set is null
                ? Def(context.obj, CompileResult.Expr(null).Add(new Del("$for")))
                : Set(context.obj, null, CompileResult.Expr(null).Add(new Del("$for"))))
            .Add(new Pop())
            .Add(Visit(context.stat()), StackEffect.Stat)
            .Add(new Outer())
            .Add(new Jump(tag), new Pop().Tagged(end))
            .WrapLoop(end, tag, context.tag()?.ID().GetText(), true);
    }
}