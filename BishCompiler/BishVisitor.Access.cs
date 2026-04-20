using BishUtils;

namespace BishCompiler;

public partial class BishVisitor
{
    private CompileResult GetExceptLast(BishParser.GetAccessContext context, string tag)
    {
        var result = CompileResult.Expr(context).Add(Visit(context.expr()), StackEffect.Expr);
        foreach (var access in context.nullAccess()[..^1])
            result.Add(Get(access, tag));
        return result;
    }

    public override CompileResult VisitGetAccess(BishParser.GetAccessContext context)
    {
        var tag = Symbols.Get("get");
        var last = context.nullAccess()[^1];
        return CompileResult.Expr(context).Add(GetExceptLast(context, tag))
            .Add(Get(last, tag)).Add(Tag(tag));
    }

    private static ListDeconstruct Deconstruct(BishParser.ArgContext[] args) =>
        new(args.Length, args.FindIndex(arg => arg is BishParser.RestArgContext), Pattern: false);

    private static IEnumerable<BishParser.ExprContext> ArgsToExpr(BishParser.ArgContext[] args)
    {
        foreach (var arg in args)
        {
            var expr = arg switch
            {
                BishParser.RestArgContext rest => rest.expr(),
                BishParser.SingleArgContext single => single.expr(),
                _ => throw new ArgumentException("impossible")
            };
            yield return expr;
        }
    }

    private CompileResult Set(string id, string? op, CompileResult value)
    {
        var tag = Symbols.Get("set");
        var result = CompileResult.Expr(null);
        switch (op)
        {
            case null:
                result.Add(value, StackEffect.Expr).Add(new Set(id));
                break;
            case "&&":
                result.Add(new Get(id), new Copy(), new JumpIfNot(tag), new Pop())
                    .Add(value, StackEffect.Expr)
                    .Add(new Set(id), Tag(tag));
                break;
            case "||":
                result.Add(new Get(id), new Copy(), new JumpIf(tag), new Pop())
                    .Add(value, StackEffect.Expr)
                    .Add(new Set(id), Tag(tag));
                break;
            case "??":
                result.Add(new Get(id), new Copy(), Op("nullish", 1), new JumpIfNot(tag), new Pop())
                    .Add(value, StackEffect.Expr)
                    .Add(new Set(id), Tag(tag));
                break;
            default:
                result.Add(new Get(id))
                    .Add(value, StackEffect.Expr)
                    .Add(Op(op, 2), new Set(id));
                break;
        }

        return result;
    }

    // Note: in `Set` and `Def`, `value` does not always evaluate at the first, so it should not rely on the stack.
    private CompileResult Set(BishParser.ExprContext context, string? op, CompileResult value)
    {
        var result = CompileResult.Expr(context);
        switch (context)
        {
            case BishParser.ListExprContext list:
            {
                var args = list.args().arg();
                result.Add(value, StackEffect.Expr).Add(Deconstruct(args));
                foreach (var (expr, i) in ArgsToExpr(args).Enumerate())
                {
                    result.Add(Set(expr, op, CompileResult.Expr(null).Add(new Del($"${i}"))));
                    if (i != args.Length - 1) result.Add(new Pop());
                }

                break;
            }
            case BishParser.MapExprContext map:
            {
                var entries = map.entries().entry();
                if (entries.SkipLast(1).Any(entry => entry is BishParser.RestEntryContext))
                    result.Error("Rest entry must be the last one in map deconstruction");
                result.Add(new GetBuiltin("map")).Add(value, StackEffect.Expr).Add(new Call(1));
                foreach (var (entry, i) in entries.Enumerate())
                    switch (entry)
                    {
                        case BishParser.SingleEntryContext single:
                            result.Add(new Copy())
                                .Add(Visit(single.key), StackEffect.Expr)
                                .Add(Op("del[]", 2), new Move($"${i}"))
                                .Add(Set(single.value, op, CompileResult.Expr(null).Add(new Del($"${i}"))))
                                .Add(new Pop());
                            break;
                        case BishParser.RestEntryContext rest:
                            result.Add(new Move($"${i}"))
                                .Add(Set(rest.expr(), op, CompileResult.Expr(null).Add(new Del($"${i}"))));
                            break;
                        default: return result.Error("Invalid set expression!");
                    }

                break;
            }
            case BishParser.ObjExprContext obj:
            {
                var entries = obj.objEntries().objEntry();
                if (entries.Any(entry => entry.expr() is not null))
                    result.Error("Entry cannot contain a value in object deconstruction");
                result.Add(value, StackEffect.Expr).Add(new Move("$_"));
                foreach (var entry in entries)
                {
                    result.Add(Set(entry.ID().GetText(), op, CompileResult.Expr(null)
                        .Add(new Get("$_"), new GetMember(entry.ID().GetText())))).Add(new Pop());
                }

                result.Add(new Del("$_"));
                break;
            }
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
                return Set(id.GetText(), op, value);
            case BishParser.GetAccessContext access:
            {
                var tag = Symbols.Get("set");
                var last = access.nullAccess()[^1];
                result.Add(GetExceptLast(access, tag));
                switch (op)
                {
                    case null:
                        result.Add(value, StackEffect.Expr).Add(Set(last, tag)).Add(Tag(tag));
                        break;
                    case "&&":
                        result.Add(new Copy())
                            .Add(Get(last, tag))
                            .Add(new Copy(), new JumpIfNot(tag), new Pop())
                            .Add(value, StackEffect.Expr)
                            .Add(Set(last, tag))
                            .Add(new Null(), new Swap(), Tag(tag), new Swap(), new Pop());
                        break;
                    case "||":
                        result.Add(new Copy())
                            .Add(Get(last, tag))
                            .Add(new Copy(), new JumpIf(tag), new Pop())
                            .Add(value, StackEffect.Expr)
                            .Add(Set(last, tag))
                            .Add(new Null(), new Swap(), Tag(tag), new Swap(), new Pop());
                        break;
                    case "??":
                        result.Add(new Copy())
                            .Add(Get(last, tag))
                            .Add(new Copy(), Op("nullish", 1), new JumpIfNot(tag), new Pop())
                            .Add(value, StackEffect.Expr)
                            .Add(Set(last, tag))
                            .Add(new Null(), new Swap(), Tag(tag), new Swap(), new Pop());
                        break;
                    default:
                        result.Add(new Copy())
                            .Add(Get(last, tag))
                            .Add(value, StackEffect.Expr)
                            .Add(Op(op, 2))
                            .Add(Set(last, tag))
                            .Add(Tag(tag));
                        break;
                }

                break;
            }
            default: return result.Error("Invalid set expression!");
        }

        return result;
    }

    private static CompileResult Def(string id, CompileResult value) =>
        CompileResult.Expr(null).Add(value, StackEffect.Expr).Add(new Def(id));

    private CompileResult Def(BishParser.ExprContext context, CompileResult value)
    {
        var result = CompileResult.Expr(context);
        switch (context)
        {
            case BishParser.ListExprContext list:
            {
                var args = list.args().arg();
                result.Add(value, StackEffect.Expr).Add(Deconstruct(args));
                foreach (var (expr, i) in ArgsToExpr(args).Enumerate())
                {
                    result.Add(Def(expr, CompileResult.Expr(null).Add(new Del($"${i}"))));
                    if (i != args.Length - 1) result.Add(new Pop());
                }

                break;
            }
            case BishParser.MapExprContext map:
            {
                var entries = map.entries().entry();
                if (entries.SkipLast(1).Any(entry => entry is BishParser.RestEntryContext))
                    result.Error("Rest entry must be the last one in map deconstruction");
                result.Add(new GetBuiltin("map")).Add(value, StackEffect.Expr).Add(new Call(1));
                foreach (var (entry, i) in entries.Enumerate())
                    switch (entry)
                    {
                        case BishParser.SingleEntryContext single:
                            result.Add(new Copy())
                                .Add(Visit(single.key), StackEffect.Expr)
                                .Add(Op("del[]", 2), new Move($"${i}"))
                                .Add(Def(single.value, CompileResult.Expr(null).Add(new Del($"${i}"))))
                                .Add(new Pop());
                            break;
                        case BishParser.RestEntryContext rest:
                            result.Add(new Move($"${i}"))
                                .Add(Def(rest.expr(), CompileResult.Expr(null).Add(new Del($"${i}"))));
                            break;
                        default: throw new ArgumentException("impossible!");
                    }

                break;
            }
            case BishParser.ObjExprContext obj:
            {
                var entries = obj.objEntries().objEntry();
                if (entries.Any(entry => entry.expr() is not null))
                    result.Error("Entry cannot contain a value in object deconstruction");
                result.Add(value, StackEffect.Expr).Add(new Move("$_"));
                foreach (var entry in entries)
                {
                    result.Add(Def(entry.ID().GetText(), CompileResult.Expr(null)
                        .Add(new Get("$_"), new GetMember(entry.ID().GetText())))).Add(new Pop());
                }

                result.Add(new Del("$_"));
                break;
            }
            case BishParser.AtomExprContext atom when atom.atom() is BishParser.IdAtomContext id:
                return Def(id.GetText(), value);
            case BishParser.GetAccessContext access:
            {
                var tag = Symbols.Get("def");
                var last = access.nullAccess()[^1];
                result.Add(GetExceptLast(access, tag)).Add(value, StackEffect.Expr).Add(Def(last, tag)).Add(Tag(tag));
                break;
            }
            default: return result.Error("Invalid def expression!");
        }

        return result;
    }

    private CompileResult Del(BishParser.ExprContext context)
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
                return CompileResult.Expr(context).Add(new Del(id.GetText()));
            case BishParser.GetAccessContext access:
                var tag = Symbols.Get("del");
                var last = access.nullAccess()[^1];
                return CompileResult.Expr(context).Add(GetExceptLast(access, tag))
                    .Add(Del(last, tag)).Add(Tag(tag));
            default: return CompileResult.Expr(context).Error("Invalid del expression!");
        }
    }

    private static CompileResult Dels(List<string> ids)
    {
        var result = CompileResult.Expr(null);
        foreach (var (id, i) in ids.Enumerate())
        {
            result.Add(new Del(id));
            if (i != ids.Count - 1) result.Add(new Pop());
        }
        return result;
    }

    private CompileResult Dels(List<BishParser.ExprContext> exprs)
    {
        var result = CompileResult.Expr(null);
        foreach (var (expr, i) in exprs.Enumerate())
        {
            result.Add(Del(expr));
            if (i != exprs.Count - 1) result.Add(new Pop());
        }
        return result;
    }

    public override CompileResult VisitSet(BishParser.SetContext context) =>
        Set(context.obj, context.setOp()?.GetText(), Visit(context.value));

    public override CompileResult VisitDef(BishParser.DefContext context) => Def(context.obj, Visit(context.value));

    public override CompileResult VisitDel(BishParser.DelContext context) => Del(context.obj);

    private CompileResult JustGet(BishParser.AccessContext access) => access switch
    {
        BishParser.MemberAccessContext member => new CompileResult(StackEffect.Trans, access)
            .Add(new GetMember(member.ID().GetText())),
        BishParser.IndexAccessContext index => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index.index()), StackEffect.Expr).Add(Op("get[]", 2)),
        BishParser.CallAccessContext call => Call(call.args().arg()),
        _ => throw new ArgumentException("impossible!")
    };

    private CompileResult Get(BishParser.NullAccessContext access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustGet(access.access()));

    private CompileResult JustSet(BishParser.AccessContext access) => access switch
    {
        BishParser.MemberAccessContext member => new CompileResult(StackEffect.Trans, access)
            .Add(new SetMember(member.ID().GetText())),
        BishParser.IndexAccessContext index => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index.index()), StackEffect.Expr).Add(new Swap(), Op("set[]", 3)),
        _ => throw new ArgumentException("impossible!")
    };

    private CompileResult Set(BishParser.NullAccessContext access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustSet(access.access()));

    private CompileResult JustDef(BishParser.AccessContext access) => access switch
    {
        BishParser.MemberAccessContext member => new CompileResult(StackEffect.Trans, access)
            .Add(new DefMember(member.ID().GetText())),
        BishParser.IndexAccessContext index => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index.index()), StackEffect.Expr).Add(new Swap(), Op("def[]", 3)),
        _ => throw new ArgumentException("impossible!")
    };

    private CompileResult Def(BishParser.NullAccessContext access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustDef(access.access()));

    private CompileResult JustDel(BishParser.AccessContext access) => access switch
    {
        BishParser.MemberAccessContext member => new CompileResult(StackEffect.Trans, access)
            .Add(new DelMember(member.ID().GetText())),
        BishParser.IndexAccessContext index => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index.index()), StackEffect.Expr).Add(Op("del[]", 2)),
        _ => throw new ArgumentException("impossible!")
    };

    private CompileResult Del(BishParser.NullAccessContext access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustDel(access.access()));

    private static CompileResult JumpIfNull(BishParser.NullAccessContext access, string tag)
    {
        var result = new CompileResult(StackEffect.Trans, access);
        if (access.op is not null) result.Add(new Copy(), Op("nullish", 1), new JumpIf(tag));
        return result;
    }
}