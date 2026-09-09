using BishUtils;

namespace BishLanguage;

public partial class BishVisitor
{
    public static void ListDeconstruct(CompileResult result, int count, int? rest)
    {
        result.Add(new Move("$_"));
        for (var i = 0; i < count; i++)
        {
            result.Add(new Get("$_"));
            switch (rest is null ? -1 : i.CompareTo(rest))
            {
                case < 0: result.Add(new Int(i)); break;
                case 0:
                    result.Add(new GetBuiltin("range"), new Int(rest!.Value), new Get("$_"), new GetMember("length"))
                        .Add(new Int(count - rest.Value - 1), Op("-", 2), new Int(1), new Call(3)); break;
                case > 0: result.Add(new Int(i - count)); break;
            }

            result.Add(Op("get[]", 2), new Move($"${i}"));
        }

        result.Add(new Del("$_"), new Pop());
    }

    public CompileResult GetExceptLast(BishParseTree tree, string tag)
    {
        if (tree is not ("GetAccess", [var expr, .. var nullAccess])) throw Impossible;
        var result = CompileResult.Expr(tree).Add(Visit(expr), StackEffect.Expr);
        foreach (var access in nullAccess[..^1])
            result.Add(Get(access, tag));
        return result;
    }

    public static IEnumerable<BishParseTree> ArgsToExpr(BishParseTree[] args)
    {
        foreach (var arg in args)
        {
            var expr = arg switch
            {
                ("RestArg", [_, var rest]) => rest,
                ("SingleArg", [var single]) => single,
                _ => throw Impossible
            };
            yield return expr;
        }
    }

    public CompileResult Set(string id, string? op, CompileResult value)
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
    public CompileResult Set(BishParseTree tree, string? op, CompileResult value)
    {
        var result = CompileResult.Expr(tree);
        switch (tree)
        {
            case ("ListExpr", [_, ("Args", var children), _]):
            {
                var args = children.Where(t => t is not { Text: "," }).ToArray();
                int? pos = null;
                foreach (var (item, i) in args.Enumerate())
                {
                    if (item is not ("RestArg", _)) continue;
                    if (pos is null) pos = i;
                    else result.Error("Found list deconstruct pattern with multiple rest pattern");
                }

                result.Add(value, StackEffect.Expr);
                ListDeconstruct(result, args.Length, pos);
                foreach (var (expr, i) in ArgsToExpr(args).Enumerate())
                {
                    result.Add(Set(expr, op, CompileResult.Expr(null).Add(new Del($"${i}"))));
                    if (i != args.Length - 1) result.Add(new Pop());
                }

                break;
            }
            case ("MapExpr", [_, ("Entries", var children), _]):
            {
                var entries = children.Where(t => t is not { Text: "," }).ToArray();
                if (entries.SkipLast(1).Any(entry => entry is ("RestEntry", _)))
                    result.Error("Rest entry must be the last one in map deconstruction");
                result.Add(new GetBuiltin("map")).Add(value, StackEffect.Expr).Add(new Call(1));
                foreach (var (entry, i) in entries.Enumerate())
                    switch (entry)
                    {
                        case ("SingleEntry", [var k, _, var v]):
                            result.Add(new Copy())
                                .Add(Visit(k), StackEffect.Expr)
                                .Add(Op("del[]", 2), new Move($"${i}"))
                                .Add(Set(v, op, CompileResult.Expr(null).Add(new Del($"${i}"))))
                                .Add(new Pop());
                            break;
                        case ("RestEntry", [_, var rest]):
                            result.Add(new Move($"${i}"))
                                .Add(Set(rest, op, CompileResult.Expr(null).Add(new Del($"${i}"))));
                            break;
                        default: return result.Error("Invalid set expression!");
                    }

                break;
            }
            case ("ObjExpr", [_, ("ObjEntries", var children), _]):
            {
                var entries = children.Where(t => t is not { Text: "," }).ToArray();
                result.Add(value, StackEffect.Expr).Add(new Move("$_"));
                foreach (var entry in entries)
                {
                    var id = IdName(entry.Children[1]);
                    var expr = entry.Children.ElementAtOrDefault(3);
                    var get = CompileResult.Expr(null).Add(new Get("$_"), new GetMember(id));
                    result.Add(expr is not null ? Set(expr, op, get) : Set(id, op, get));
                    result.Add(new Pop());
                }

                result.Add(new Del("$_"));
                break;
            }
            case ("AtomExpr", [("IdAtom", [var id])]):
                return Set(IdName(id), op, value).WithTree(tree);
            case ("GetAccess", [_, .. var nullAccess]):
            {
                var tag = Symbols.Get("set");
                var last = nullAccess[^1];
                result.Add(GetExceptLast(tree, tag));
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

    public static CompileResult Def(string id, CompileResult value) =>
        CompileResult.Expr(null).Add(value, StackEffect.Expr).Add(new Def(id));

    public CompileResult Def(BishParseTree tree, CompileResult value)
    {
        var result = CompileResult.Expr(tree);
        switch (tree)
        {
            case ("ListExpr", [_, ("Args", var children), _]):
            {
                var args = children.Where(t => t is not { Text: "," }).ToArray();
                int? pos = null;
                foreach (var (item, i) in args.Enumerate())
                {
                    if (item is not ("RestArg", _)) continue;
                    if (pos is null) pos = i;
                    else result.Error("Found list deconstruct pattern with multiple rest pattern");
                }

                result.Add(value, StackEffect.Expr);
                ListDeconstruct(result, args.Length, pos);
                foreach (var (expr, i) in ArgsToExpr(args).Enumerate())
                {
                    result.Add(Def(expr, CompileResult.Expr(null).Add(new Del($"${i}"))));
                    if (i != args.Length - 1) result.Add(new Pop());
                }

                break;
            }
            case ("MapExpr", [_, ("Entries", var children), _]):
            {
                var entries = children.Where(t => t is not { Text: "," }).ToArray();
                if (entries.SkipLast(1).Any(entry => entry is ("RestEntry", _)))
                    result.Error("Rest entry must be the last one in map deconstruction");
                result.Add(new GetBuiltin("map")).Add(value, StackEffect.Expr).Add(new Call(1));
                foreach (var (entry, i) in entries.Enumerate())
                    switch (entry)
                    {
                        case ("SingleEntry", [var k, _, var v]):
                            result.Add(new Copy())
                                .Add(Visit(k), StackEffect.Expr)
                                .Add(Op("del[]", 2), new Move($"${i}"))
                                .Add(Def(v, CompileResult.Expr(null).Add(new Del($"${i}"))))
                                .Add(new Pop());
                            break;
                        case ("RestEntry", [_, var rest]):
                            result.Add(new Move($"${i}"))
                                .Add(Def(rest, CompileResult.Expr(null).Add(new Del($"${i}"))));
                            break;
                        default: throw Impossible;
                    }

                break;
            }
            case ("ObjExpr", [_, ("ObjEntries", var children), _]):
            {
                var entries = children.Where(t => t is not { Text: "," }).ToArray();
                result.Add(value, StackEffect.Expr).Add(new Move("$_"));
                foreach (var entry in entries)
                {
                    var id = IdName(entry.Children[1]);
                    var expr = entry.Children.ElementAtOrDefault(3);
                    var get = CompileResult.Expr(null).Add(new Get("$_"), new GetMember(id));
                    result.Add(expr is not null ? Def(expr, get) : Def(id, get));
                    result.Add(new Pop());
                }

                result.Add(new Del("$_"));
                break;
            }
            case ("AtomExpr", [("IdAtom", [var id])]):
                return Def(IdName(id), value).WithTree(tree);
            case ("GetAccess", [_, .. var nullAccess]):
            {
                var tag = Symbols.Get("def");
                var last = nullAccess[^1];
                result.Add(GetExceptLast(tree, tag)).Add(value, StackEffect.Expr).Add(Def(last, tag)).Add(Tag(tag));
                break;
            }
            default: return result.Error("Invalid def expression!");
        }

        return result;
    }

    public CompileResult Del(BishParseTree tree)
    {
        switch (tree)
        {
            case ("ListExpr", [_, ("Args", var children), _]):
                return Dels(ArgsToExpr(children.Where(t => t is not { Text: "," }).ToArray()).ToList()).WithTree(tree);
            case ("MapExpr", [_, ("Entries", var children), _]):
                return Dels(children.Where(t => t is not { Text: "," }).Select(entry => entry switch
                {
                    ("SingleEntry", [var _, _, var value]) => value,
                    ("RestEntry", [_, var rest]) => rest,
                    _ => throw Impossible
                }).ToList()).WithTree(tree);
            case ("ObjExpr", [_, ("ObjEntries", var children), _]):
                return Dels(children.Where(t => t is not { Text: "," })
                    .Select(entry => IdName(entry.Children[1])).ToList()).WithTree(tree);
            case ("AtomExpr", [("IdAtom", [var id])]):
                return CompileResult.Expr(tree).Add(new Del(IdName(id)));
            case ("GetAccess", [_, .. var nullAccess]):
                var tag = Symbols.Get("del");
                var last = nullAccess[^1];
                return CompileResult.Expr(tree).Add(GetExceptLast(tree, tag))
                    .Add(Del(last, tag)).Add(Tag(tag));
            default: return CompileResult.Expr(tree).Error("Invalid del expression!");
        }
    }

    public static CompileResult Dels(List<string> ids)
    {
        var result = CompileResult.Expr(null);
        foreach (var (id, i) in ids.Enumerate())
        {
            result.Add(new Del(id));
            if (i != ids.Count - 1) result.Add(new Pop());
        }

        return result;
    }

    public CompileResult Dels(List<BishParseTree> exprs)
    {
        var result = CompileResult.Expr(null);
        foreach (var (expr, i) in exprs.Enumerate())
        {
            result.Add(Del(expr));
            if (i != exprs.Count - 1) result.Add(new Pop());
        }

        return result;
    }

    public CompileResult JustGet(BishParseTree access) => access switch
    {
        ("MemberAccess", [_, var member]) => new CompileResult(StackEffect.Trans, access)
            .Add(new GetMember(IdName(member))),
        ("IndexAccess", [var index]) => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index), StackEffect.Expr).Add(Op("get[]", 2)),
        ("CallAccess", [_, ("Args", var children), _]) =>
            Call(children.Where(t => t is not { Text: "," }).ToArray()).WithTree(access),
        _ => throw Impossible
    };

    public CompileResult Get(BishParseTree access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustGet(access.Children[^1]));

    public CompileResult JustSet(BishParseTree access) => access switch
    {
        ("MemberAccess", [_, var member]) => new CompileResult(StackEffect.Trans, access)
            .Add(new SetMember(IdName(member))),
        ("IndexAccess", [var index]) => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index), StackEffect.Expr).Add(new Swap(), Op("set[]", 3)),
        _ => throw Impossible
    };

    public CompileResult Set(BishParseTree access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustSet(access.Children[^1]));

    public CompileResult JustDef(BishParseTree access) => access switch
    {
        ("MemberAccess", [_, var member]) => new CompileResult(StackEffect.Trans, access)
            .Add(new DefMember(IdName(member))),
        ("IndexAccess", [var index]) => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index), StackEffect.Expr).Add(new Swap(), Op("def[]", 3)),
        _ => throw Impossible
    };

    public CompileResult Def(BishParseTree access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustDef(access.Children[^1]));

    public CompileResult JustDel(BishParseTree access) => access switch
    {
        ("MemberAccess", [_, var member]) => new CompileResult(StackEffect.Trans, access)
            .Add(new DelMember(IdName(member))),
        ("IndexAccess", [var index]) => new CompileResult(StackEffect.Trans, access)
            .Add(Visit(index), StackEffect.Expr).Add(Op("del[]", 2)),
        _ => throw Impossible
    };

    public CompileResult Del(BishParseTree access, string tag) =>
        new CompileResult(StackEffect.Trans, access).Add(JumpIfNull(access, tag)).Add(JustDel(access.Children[^1]));

    public static CompileResult JumpIfNull(BishParseTree access, string tag)
    {
        var result = new CompileResult(StackEffect.Trans, access);
        if (access.Children.Count == 2) result.Add(new Copy(), Op("nullish", 1), new JumpIf(tag));
        return result;
    }
}