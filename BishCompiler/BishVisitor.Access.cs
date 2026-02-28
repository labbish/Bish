using BishBytecode.Bytecodes;

namespace BishCompiler;

public partial class BishVisitor
{
    private Codes GetExceptLast(BishParser.GetAccessContext context, string tag)
        => [..Visit(context.expr()), ..context.nullAccess()[..^1].SelectMany(access => Get(access, tag))];

    public override Codes VisitGetAccess(BishParser.GetAccessContext context)
    {
        var tag = Symbols.Get("get");
        var last = context.nullAccess()[^1];
        return [..GetExceptLast(context, tag), ..Get(last, tag), Tag(tag)];
    }

    private static ListDeconstruct Deconstruct(BishParser.ArgContext[] args) =>
        new(args.Length, args.ToList().FindIndex(arg => arg is BishParser.RestArgContext), Pattern: false);

    private IEnumerable<BishParser.ExprContext> ArgsToExpr(BishParser.ArgContext[] args)
    {
        foreach (var arg in args)
        {
            var expr = arg switch
            {
                BishParser.RestArgContext rest => rest.expr(),
                BishParser.SingleArgContext single => single.expr(),
                _ => null
            };
            if (expr is null)
            {
                Error(arg, "Invalid argument type");
                yield break;
            }

            yield return expr;
        }
    }

    private Codes Set(BishParser.ExprContext obj, string? op, Codes value)
    {
        switch (obj)
        {
            case BishParser.ListExprContext list:
            {
                var args = list.args().arg();
                return
                [
                    ..value,
                    Deconstruct(args),
                    ..ArgsToExpr(args).SelectMany((expr, i) =>
                        Set(expr, op, [new Del($"${i}")]).Concat([i == args.Length - 1 ? new Nop() : new Pop()]))
                ];
            }
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
            {
                var tag = Symbols.Get("set");
                var name = id.GetText();
                return op switch
                {
                    null => [..value, new Set(name)],
                    "&&" =>
                        [new Get(name), new Copy(), new JumpIfNot(tag), new Pop(), ..value, new Set(name), Tag(tag)],
                    "||" => [new Get(name), new Copy(), new JumpIf(tag), new Pop(), ..value, new Set(name), Tag(tag)],
                    "??" =>
                    [
                        new Get(name), new Copy(), new IsNull(), new JumpIfNot(tag),
                        new Pop(), ..value, new Set(name), Tag(tag)
                    ],
                    _ => [new Get(name), ..value, Op(op, 2), new Set(name)]
                };
            }
            case BishParser.GetAccessContext access:
            {
                var tag = Symbols.Get("set");
                var last = access.nullAccess()[^1];
                return op switch
                {
                    null => [..GetExceptLast(access, tag), ..value, ..Set(last, tag), Tag(tag)],
                    "&&" =>
                    [
                        ..GetExceptLast(access, tag), new Copy(), ..Get(last, tag), new Copy(), new JumpIfNot(tag),
                        new Pop(), ..value, ..Set(last, tag), new Null(), new Swap(), Tag(tag), new Swap(), new Pop()
                    ],
                    "||" =>
                    [
                        ..GetExceptLast(access, tag), new Copy(), ..Get(last, tag), new Copy(), new JumpIf(tag),
                        new Pop(), ..value, ..Set(last, tag), new Null(), new Swap(), Tag(tag), new Swap(), new Pop()
                    ],
                    "??" =>
                    [
                        ..GetExceptLast(access, tag), new Copy(), ..Get(last, tag), new Copy(),
                        new IsNull(), new JumpIfNot(tag), new Pop(), ..value,
                        ..Set(last, tag), new Null(), new Swap(), Tag(tag), new Swap(), new Pop()
                    ],
                    _ =>
                    [
                        ..GetExceptLast(access, tag), new Copy(), ..Get(last, tag),
                        ..value, Op(op, 2), ..Set(last, tag), Tag(tag)
                    ]
                };
            }
        }

        return Error(obj, "Invalid set expression");
    }

    private Codes Def(BishParser.ExprContext obj, Codes value)
    {
        switch (obj)
        {
            case BishParser.ListExprContext list:
            {
                var args = list.args().arg();
                return
                [
                    ..value,
                    Deconstruct(args),
                    ..ArgsToExpr(args).SelectMany((expr, i) =>
                        Def(expr, [new Del($"${i}")]).Concat([i == args.Length - 1 ? new Nop() : new Pop()]))
                ];
            }
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
                return [..value, new Def(id.GetText())];
            case BishParser.GetAccessContext access:
                var tag = Symbols.Get("def");
                var last = access.nullAccess()[^1];
                return [..GetExceptLast(access, tag), ..value, ..Set(last, tag), Tag(tag)];
        }

        return Error(obj, "Invalid def expression");
    }

    private Codes Del(BishParser.ExprContext obj)
    {
        switch (obj)
        {
            case BishParser.ListExprContext list:
            {
                var args = list.args().arg();
                return ArgsToExpr(args).SelectMany((expr, i) =>
                    Del(expr).Concat([i == args.Length - 1 ? new Nop() : new Pop()])).ToList();
            }
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
                return [new Del(id.GetText())];
            case BishParser.GetAccessContext access:
                var tag = Symbols.Get("del");
                var last = access.nullAccess()[^1];
                return [..GetExceptLast(access, tag), ..Del(last, tag), Tag(tag)];
        }

        return Error(obj, "Invalid del expression");
    }

    public override Codes VisitSet(BishParser.SetContext context) =>
        Set(context.obj, context.setOp()?.GetText(), Visit(context.value));

    public override Codes VisitDef(BishParser.DefContext context) => Def(context.obj, Visit(context.value));

    public override Codes VisitDel(BishParser.DelContext context) => Del(context.obj);

    private Codes JustGet(BishParser.AccessContext access) => access switch
    {
        BishParser.MemberAccessContext member => [new GetMember(member.ID().GetText())],
        BishParser.IndexAccessContext index => [..Visit(index.index()), Op("get[]", 2)],
        BishParser.CallAccessContext call => Call(call.args().arg()),
        _ => Error(access, "Invalid get expression")
    };

    private Codes Get(BishParser.NullAccessContext access, string tag) =>
        [..JumpIfNull(access, tag), ..JustGet(access.access())];

    private Codes JustSet(BishParser.AccessContext access) => access switch
    {
        BishParser.MemberAccessContext member => [new SetMember(member.ID().GetText())],
        BishParser.IndexAccessContext index => [..Visit(index.index()), new Swap(), Op("set[]", 3)],
        _ => Error(access, "Invalid set expression")
    };

    private Codes Set(BishParser.NullAccessContext access, string tag) =>
        [..JumpIfNull(access, tag), ..JustSet(access.access())];

    private Codes JustDel(BishParser.AccessContext access) => access switch
    {
        BishParser.MemberAccessContext member => [new DelMember(member.ID().GetText())],
        BishParser.IndexAccessContext index => [..Visit(index.index()), Op("del[]", 2)],
        _ => Error(access, "Invalid del expression")
    };

    private Codes Del(BishParser.NullAccessContext access, string tag) =>
        [..JumpIfNull(access, tag), ..JustDel(access.access())];

    private static Codes JumpIfNull(BishParser.NullAccessContext access, string tag) =>
        access.op is null ? [] : [new Copy(), new IsNull(), new JumpIf(tag)];
}