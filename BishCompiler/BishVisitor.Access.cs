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

    private Codes Set(string id, string? op, Codes value)
    {
        var tag = Symbols.Get("set");
        return op switch
        {
            null => [..value, new Set(id)],
            "&&" =>
                [new Get(id), new Copy(), new JumpIfNot(tag), new Pop(), ..value, new Set(id), Tag(tag)],
            "||" => [new Get(id), new Copy(), new JumpIf(tag), new Pop(), ..value, new Set(id), Tag(tag)],
            "??" =>
            [
                new Get(id), new Copy(), new IsNull(), new JumpIfNot(tag),
                new Pop(), ..value, new Set(id), Tag(tag)
            ],
            _ => [new Get(id), ..value, Op(op, 2), new Set(id)]
        };
    }

    // Note: in `Set` and `Def`, `value` does not always evaluate at the first, so it should not rely on the stack.
    private Codes Set(BishParser.ExprContext context, string? op, Codes value)
    {
        switch (context)
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
            case BishParser.MapExprContext map:
            {
                var entries = map.entries().entry();
                if (entries.SkipLast(1).Any(entry => entry is BishParser.RestEntryContext))
                    return Error(context, "Rest entry must be the last one in map deconstruction");
                return
                [
                    new Get("map"),
                    ..value,
                    new Call(1),
                    ..entries.SelectMany((entry, i) => entry switch
                    {
                        BishParser.SingleEntryContext single => (Codes)(
                        [
                            new Copy(),
                            ..Visit(single.key),
                            Op("del[]", 2),
                            new Move($"${i}"),
                            ..Set(single.value, op, [new Del($"${i}")]),
                            new Pop()
                        ]),
                        BishParser.RestEntryContext rest =>
                        [
                            new Move($"${i}"),
                            ..Set(rest.expr(), op, [new Del($"${i}")])
                        ],
                        _ => throw new ArgumentException("Invalid entry!")
                    })
                ];
            }
            case BishParser.ObjExprContext obj:
            {
                var entries = obj.objEntries().objEntry();
                if (entries.Any(entry => entry.expr() is not null))
                    return Error(obj, "Entry cannot contain a value in object deconstruction");
                return
                [
                    ..value,
                    new Move("$_"),
                    ..entries.SelectMany(entry => Set(entry.ID().GetText(), op,
                        [new Get("$_"), new GetMember(entry.ID().GetText())]).Concat([new Pop()])),
                    new Del("$_")
                ];
            }
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
                return Set(id.GetText(), op, value);
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

        return Error(context, "Invalid set expression");
    }

    private static Codes Def(string id, Codes value) => [..value, new Def(id)];

    private Codes Def(BishParser.ExprContext context, Codes value)
    {
        switch (context)
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
            case BishParser.MapExprContext map:
            {
                var entries = map.entries().entry();
                if (entries.SkipLast(1).Any(entry => entry is BishParser.RestEntryContext))
                    return Error(context, "Rest entry must be the last one in map deconstruction");
                return
                [
                    new Get("map"),
                    ..value,
                    new Call(1),
                    ..entries.SelectMany((entry, i) => entry switch
                    {
                        BishParser.SingleEntryContext single => (Codes)(
                        [
                            new Copy(),
                            ..Visit(single.key),
                            Op("del[]", 2),
                            new Move($"${i}"),
                            ..Def(single.value, [new Del($"${i}")]),
                            new Pop()
                        ]),
                        BishParser.RestEntryContext rest =>
                        [
                            new Move($"${i}"),
                            ..Def(rest.expr(), [new Del($"${i}")])
                        ],
                        _ => throw new ArgumentException("Invalid entry!")
                    })
                ];
            }
            case BishParser.ObjExprContext obj:
            {
                var entries = obj.objEntries().objEntry();
                if (entries.Any(entry => entry.expr() is not null))
                    return Error(obj, "Entry cannot contain a value in object deconstruction");
                return
                [
                    ..value,
                    new Move("$_"),
                    ..entries.SelectMany(entry => Def(entry.ID().GetText(),
                        [new Get("$_"), new GetMember(entry.ID().GetText())]).Concat([new Pop()])),
                    new Del("$_")
                ];
            }
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
                return Def(id.GetText(), value);
            case BishParser.GetAccessContext access:
                var tag = Symbols.Get("def");
                var last = access.nullAccess()[^1];
                return [..GetExceptLast(access, tag), ..value, ..Set(last, tag), Tag(tag)];
        }

        return Error(context, "Invalid def expression");
    }

    private Codes Del(BishParser.ExprContext context)
    {
        switch (context)
        {
            case BishParser.ListExprContext list:
                return Dels(ArgsToExpr(list.args().arg()).ToList());
            case BishParser.MapExprContext map:
                return Dels(map.entries().entry().Select(entry => entry switch
                {
                    BishParser.SingleEntryContext single => single.value,
                    BishParser.RestEntryContext rest => rest.expr(),
                    _ => throw new ArgumentException("Invalid entry!")
                }).ToList());
            case BishParser.ObjExprContext obj:
                return Dels(obj.objEntries().objEntry().Select(entry => entry.ID().GetText()).ToList());
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
                return [new Del(id.GetText())];
            case BishParser.GetAccessContext access:
                var tag = Symbols.Get("del");
                var last = access.nullAccess()[^1];
                return [..GetExceptLast(access, tag), ..Del(last, tag), Tag(tag)];
        }

        return Error(context, "Invalid del expression");
    }

    private static Codes Dels(List<string> ids) => ids.SelectMany((id, i) =>
        (Codes)([new Del(id), i == ids.Count - 1 ? new Nop() : new Pop()])).ToList();

    private Codes Dels(List<BishParser.ExprContext> exprs) => exprs.SelectMany((expr, i) =>
        Del(expr).Concat([i == exprs.Count - 1 ? new Nop() : new Pop()])).ToList();

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