using Antlr4.Runtime;
using BishBytecode.Bytecodes;

namespace BishCompiler;

public partial class BishVisitor
{
    public override Codes VisitIfStat(BishParser.IfStatContext context) =>
        Condition("if", Visit(context.cond), Wrap(Visit(context.left)),
            context.right is null ? [] : Wrap(Visit(context.right)));

    private static Codes WrapLoop(Codes codes, string @break, string @continue, string? loopTag, bool pops = false) =>
        Wrap(codes).SelectMany(code => code switch
        {
            Break x when MatchLoopTag(x.LoopTag, loopTag) =>
                [..Enumerable.Repeat(new Pop(), x.Depth), new Jump(@break)],
            Continue x when MatchLoopTag(x.LoopTag, loopTag) =>
                [..Enumerable.Repeat(new Pop(), x.Depth), new Jump(@continue)],
            LoopUnbound x when pops => [x.Deeper()],
            _ => (Codes)([code])
        }).ToList();

    private static bool MatchLoopTag(string? unbound, string? loop) => unbound is null || unbound == loop;

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

    public override Codes VisitBreakStat(BishParser.BreakStatContext context) =>
        [new Break(context, context.ID()?.GetText())];

    internal record Break(ParserRuleContext Context, string? LoopTag) : LoopUnbound(Context, "break", LoopTag);

    public override Codes VisitContinueStat(BishParser.ContinueStatContext context) =>
        [new Continue(context, context.ID()?.GetText())];

    internal record Continue(ParserRuleContext Context, string? LoopTag) : LoopUnbound(Context, "continue", LoopTag);

    public override Codes VisitWhileStat(BishParser.WhileStatContext context)
    {
        var (tag, end) = Symbols.GetPair("while");
        return WrapLoop([
            Tag(tag),
            ..Visit(context.expr()),
            new JumpIfNot(end),
            ..Wrap(Visit(context.stat())),
            new Jump(tag),
            Tag(end)
        ], end, tag, context.tag()?.ID().GetText());
    }

    public override Codes VisitDoWhileStat(BishParser.DoWhileStatContext context)
    {
        var (tag, end) = Symbols.GetPair("do_while");
        var @continue = Symbols.Get("do_while_continue");
        return WrapLoop([
            Tag(tag),
            ..Wrap(Visit(context.stat())),
            Tag(@continue),
            ..Visit(context.expr()),
            new JumpIf(tag),
            Tag(end)
        ], end, @continue, context.tag()?.ID().GetText());
    }

    public override Codes VisitForStat(BishParser.ForStatContext context)
    {
        var (tag, end) = Symbols.GetPair("for");
        var @continue = Symbols.Get("for_continue");
        return WrapLoop([
            ..Visit(context.forStats().init),
            Tag(tag),
            ..Visit(context.forStats().cond),
            new JumpIfNot(end),
            ..Wrap(Visit(context.stat()), [Tag(@continue)], Visit(context.forStats().step)),
            new Pop(),
            new Jump(tag),
            new Pop().Tagged(end)
        ], end, @continue, context.tag()?.ID().GetText(), true);
    }

    public override Codes VisitForIterStat(BishParser.ForIterStatContext context)
    {
        var (tag, end) = Symbols.GetPair("for_iter");
        var name = context.name.Text;
        return WrapLoop([
            ..Visit(context.expr()),
            new Op("iter", 1),
            new ForIter(end).Tagged(tag),
            ..Wrap([context.set is null ? new Def(name) : new Set(name), new Pop()], Visit(context.stat())),
            new Jump(tag),
            new Pop().Tagged(end)
        ], end, tag, context.tag()?.ID().GetText(), true);
    }
}