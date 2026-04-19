using System.Text.RegularExpressions;
using String = BishRuntime.String;

namespace BishCompiler;

public partial class BishVisitor : BishBaseVisitor<CompileResult>
{
    protected readonly SymbolAllocator Symbols = new();

    private static BishBytecode Tag(string tag) => new Nop().Tagged(tag);

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

    private static string ToStr(string text)
    {
        var raw = text.StartsWith('r');
        var str = text.TrimStart('r').Trim('#')[1..^1];
        return raw ? str : Regex.Unescape(str);
    }

    public override CompileResult VisitIntAtom(BishParser.IntAtomContext context) =>
        CompileResult.Expr(context).TryAdd(() => new Int(ToInt(context.INT().GetText())));

    public override CompileResult VisitNumAtom(BishParser.NumAtomContext context) =>
        CompileResult.Expr(context).TryAdd(() => new Num(ToNum(context.NUM().GetText())));

    public override CompileResult VisitStrAtom(BishParser.StrAtomContext context) =>
        CompileResult.Expr(context).TryAdd(() => new String(ToStr(context.STR().GetText())));

    public override CompileResult VisitNullAtom(BishParser.NullAtomContext context) =>
        CompileResult.Expr(context).Add(new Null());

    public override CompileResult VisitBoolAtom(BishParser.BoolAtomContext context) =>
        CompileResult.Expr(context).Add(new Bool(context.BOL().GetText() == "true"));

    public override CompileResult VisitIdAtom(BishParser.IdAtomContext context) =>
        CompileResult.Expr(context).Add(new Get(context.GetText()));

    public override CompileResult VisitParenExpr(BishParser.ParenExprContext context) => Visit(context.expr());

    private static Op Op(string op, int argc) => new(BishOperator.GetOperatorName(op, argc), argc);

    public override CompileResult VisitUnOpExpr(BishParser.UnOpExprContext context) =>
        CompileResult.Expr(context).Add(Visit(context.expr()), StackEffect.Expr)
            .Add(context.op.Text == "!" ? new Not() : Op(context.op.Text, 1));

    public override CompileResult VisitBinOpExpr(BishParser.BinOpExprContext context) =>
        CompileResult.Expr(context)
            .Add(Visit(context.left), StackEffect.Expr)
            .Add(Visit(context.right), StackEffect.Expr)
            .Add(context.op.Text switch
            {
                "===" => [new RefEq()],
                "!==" => [new RefEq(), new Not()],
                { } op => [Op(op, 2)]
            });

    public override CompileResult VisitSingleIndex(BishParser.SingleIndexContext context) =>
        new CompileResult(StackEffect.Expr, context).Add(Visit(context.expr()), StackEffect.Expr);

    private CompileResult VisitOrNull(BishParser.ExprContext? expr) => expr is null
        ? new CompileResult(StackEffect.Expr, expr).Add(new Null())
        : new CompileResult(StackEffect.Expr, expr).Add(Visit(expr), StackEffect.Expr);

    public override CompileResult VisitRangeIndex(BishParser.RangeIndexContext context)
    {
        var result = new CompileResult(StackEffect.Expr, context).Add(new GetBuiltin("range"));
        result.Add(VisitOrNull(context.start), StackEffect.Expr);
        result.Add(VisitOrNull(context.end), StackEffect.Expr);
        result.Add(VisitOrNull(context.step), StackEffect.Expr);
        result.Add(new Call(3));
        return result;
    }

    public override CompileResult VisitLogicAndExpr(BishParser.LogicAndExprContext context)
    {
        var result = CompileResult.Expr(context);
        var tag = Symbols.Get("bin_and");
        return result.Add(Visit(context.left))
            .Add(new Op("bool", 1), new Copy(), new JumpIfNot(tag), new Pop())
            .Add(Visit(context.right))
            .Add(Tag(tag));
    }

    public override CompileResult VisitLogicOrExpr(BishParser.LogicOrExprContext context)
    {
        var result = CompileResult.Expr(context);
        var tag = Symbols.Get("bin_or");
        return result.Add(Visit(context.left))
            .Add(new Op("bool", 1), new Copy(), new JumpIf(tag), new Pop())
            .Add(Visit(context.right))
            .Add(Tag(tag));
    }

    public override CompileResult VisitNullCombExpr(BishParser.NullCombExprContext context)
    {
        var result = CompileResult.Expr(context);
        var tag = Symbols.Get("null_comb");
        return result.Add(Visit(context.left))
            .Add(new Copy(), new IsNull(), new JumpIfNot(tag), new Pop())
            .Add(Visit(context.right))
            .Add(Tag(tag));
    }

    private CompileResult Condition(string name, CompileResult cond, CompileResult left, CompileResult? right)
    {
        if (right is null)
        {
            right = CompileResult.Same(null, left);
            if (left.Effect == StackEffect.Expr) right.Add(new Null());
        }
        var result = CompileResult.Same(null, left, right);
        var (tag, end) = Symbols.GetPair(name);
        return result.Add(cond, StackEffect.Expr)
            .Add(new JumpIfNot(tag))
            .Add(left)
            .Add(new Jump(end))
            .Add(Tag(tag))
            .Add(right)
            .Add(Tag(end))
            .Wrap();
    }

    private CompileResult Call(BishParser.ArgContext[] args)
    {
        if (HasRest(args)) return ToList(args).Add(new CallArgs());
        var result = new CompileResult(StackEffect.Trans, null);
        foreach (var arg in args) result.Add(Visit(arg), StackEffect.Expr);
        result.Add(new Call(args.Length));
        return result;
    }

    public override CompileResult VisitListExpr(BishParser.ListExprContext context)
    {
        var args = context.args().arg();
        if (HasRest(args)) return ToList(args);
        var result = CompileResult.Expr(context);
        foreach (var arg in args) result.Add(Visit(arg), StackEffect.Expr);
        result.Add(new BuildList(args.Length));
        return result;
    }

    protected CompileResult ToList(BishParser.ArgContext[] args)
    {
        var result = CompileResult.Expr(null).Add(new BuildList(0));
        foreach (var arg in args)
        {
            result.Add(Visit(arg), StackEffect.Expr);
            result.Add(arg switch
            {
                BishParser.RestArgContext => [new GetBuiltin("list"), new Swap(), new Call(1), new Op("op_add", 2)],
                _ => [new Swap(), new GetMember("add"), new Swap(), new Call(1)]
            });
        }

        return result;
    }

    protected static bool HasRest(BishParser.ArgContext[] args) => args.Any(arg => arg is BishParser.RestArgContext);

    public override CompileResult VisitMapExpr(BishParser.MapExprContext context)
    {
        var result = CompileResult.Expr(context).Add(new GetBuiltin("map"), new Call(0));
        foreach (var entry in context.entries().entry())
            switch (entry)
            {
                case BishParser.RestEntryContext rest:
                    result.Add(Visit(rest.expr()), StackEffect.Expr);
                    result.Add(Op("+", 2));
                    break;
                case BishParser.SingleEntryContext single:
                    result.Add(new Copy());
                    result.Add(Visit(single.key), StackEffect.Expr);
                    result.Add(Visit(single.value), StackEffect.Expr);
                    result.Add(Op("def[]", 3), new Pop());
                    break;
                default: throw new ArgumentException("impossible");
            }

        return result;
    }

    public override CompileResult VisitObjExpr(BishParser.ObjExprContext context)
    {
        var result = CompileResult.Expr(context).Add(new GetBuiltin("object"), new Call(0));
        foreach (var entry in context.objEntries().objEntry())
        {
            result.Add(new Copy());
            if (entry.expr() is null) result.Add(new Get(entry.ID().GetText()));
            else result.Add(Visit(entry.expr()), StackEffect.Expr);
            result.Add(new DefMember(entry.ID().GetText()), new Pop());
        }

        return result;
    }

    public CompileResult VisitMulti(IList<BishParser.ExprContext> fronts, BishParser.ExprContext? last)
    {
        var expr = last is null ? CompileResult.Stat(last) : Visit(last);
        var result = CompileResult.Same(null, expr);
        foreach (var front in fronts) result.Add(Visit(front).IntoStat());
        return result.Add(expr);
    }

    public override CompileResult VisitBlockExpr(BishParser.BlockExprContext context) =>
        VisitMulti(context._front, context.last).Wrap();

    public override CompileResult VisitProgram(BishParser.ProgramContext context) =>
        VisitMulti(context._front, context.last);

    public CompileResult VisitFull(IParseTree tree, bool optimize) => Visit(tree).Full(optimize);
}

public static class EnumeratorHelper
{
    extension<T>(IEnumerable<T> enumerable)
    {
        public IEnumerable<(T, int)> Enumerate() => enumerable.Select((x, i) => (x, i));
    }
}

public static class CompileResultHelper
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

        internal CompileResult IntoStat() => result.Effect switch
        {
            StackEffect.Expr => CompileResult.Stat(result.Context).Add(result).Add(new Pop()),
            StackEffect.Stat => result,
            _ => throw new ArgumentException("impossible!")
        };

        internal CompileResult IntoReturn() => result.Effect switch
        {
            StackEffect.Expr => CompileResult.Stat(result.Context).Add(result).Add(new Ret()),
            StackEffect.Stat => result,
            _ => throw new ArgumentException("impossible!")
        };
    }

    private static bool MatchLoopTag(string? unbound, string? loop) => unbound is null || unbound == loop;
}