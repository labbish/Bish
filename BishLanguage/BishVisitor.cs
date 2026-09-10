using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using BishUtils;
using String = BishRuntime.String;

namespace BishLanguage;

public partial class BishVisitor(ICodeSource? source)
{
    protected readonly BishScope Scope = BishScope.Globals.AddMeta(source?.Root);
    protected readonly SymbolAllocator Symbols = new();

    public const string Anonymous = "anonymous";

    public static BishBytecode Tag(string tag) => new Nop().Tagged(tag);

    public static int ToInt(string text)
    {
        if (text[0] != '0' || text.Length < 2) return int.Parse(text);
        var radix = text[1] switch { 'x' => 16, 'o' => 8, 'b' => 2, _ => 10 };
        return Convert.ToInt32(text[2..], radix);
    }

    public static double ToNum(string text)
    {
        var pos = text.IndexOf('e');
        if (pos == -1) return double.Parse(text);
        var part = text[(pos + 1)..];
        var exp = "+-".Contains(part[0]) ? ToInt(part[1..]) * (part[0] == '-' ? -1 : 1) : ToInt(part);
        return double.Parse(text[..pos]) * Math.Pow(10, exp);
    }

    public static string ToStr(string text)
    {
        var raw = text.StartsWith('r');
        var str = text.TrimStart('r').Trim('#')[1..^1];
        return raw ? str : Regex.Unescape(str);
    }

    public static string IdName(BishParseTree id) => id switch
    {
        ("SimpleId", [{ Text: { } text }]) => text,
        ("StrId", [_, _, { Text: { } text }, _]) => ToStr(text),
        _ => throw Impossible
    };

    public static Op Op(string op, int argc) => new(BishOperator.GetOperatorName(op, argc), argc);

    public CompileResult OrNull(BishParseTree? expr) => expr is null
        ? new CompileResult(StackEffect.Expr, expr).Add(new Null())
        : new CompileResult(StackEffect.Expr, expr).Add(Visit(expr), StackEffect.Expr);

    public CompileResult Condition(string name, CompileResult cond, CompileResult left, CompileResult? right)
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

    public CompileResult Call(BishParseTree[] args)
    {
        if (HasRest(args)) return ToList(args).Add(new CallArgs());
        var result = new CompileResult(StackEffect.Trans, null);
        foreach (var arg in args) result.Add(Visit(arg), StackEffect.Expr);
        result.Add(new Call(args.Length));
        return result;
    }

    public CompileResult ToList(BishParseTree[] args)
    {
        var result = CompileResult.Expr(null).Add(new BuildList(0));
        foreach (var arg in args)
        {
            result.Add(Visit(arg), StackEffect.Expr);
            result.Add(arg switch
            {
                ("RestArg", _) => [new GetBuiltin("list"), new Swap(), new Call(1), new Op("op_add", 2)],
                _ => [new Swap(), new GetMember("add"), new Swap(), new Call(1)]
            });
        }

        return result;
    }

    public static bool HasRest(BishParseTree[] args) => args.Any(arg => arg is ("RestArg", _));

    public CompileResult ForIter(BishParseTree tree, CompileResult body, string? loopTag, bool await = false)
    {
        var (tag, end) = Symbols.GetPair("for_iter");
        var @break = Symbols.Get("for_iter_break");
        var result = new CompileResult(StackEffect.Trans, tree)
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

    public CompileResult MakeFunc(CompileResult result, string? name, BishParseTree body,
        IList<BishParseTree> decos, bool fixedArgc = false, string funcName = "")
    {
        string ArgName(BishParseTree obj, int i) =>
            obj is ("AtomExpr", [("IdAtom", [var id])]) ? IdName(id) : $"$arg{i}";

        if (body is not ("FuncBody", [_, ("DefArgs", var defArgs), _, .. var rest, var expr])) throw Impossible;
        defArgs = defArgs.Where(t => t is not { Text: "," }).ToList();
        var symbol = Symbols.Get(name ?? Anonymous);
        var args = result.Try(() => BishFunc.CheckedArgs<Arg<BishParseTree>, BishParseTree>(defArgs
            .Select((arg, i) =>
            {
                var isRest = arg.Children[0] is { Text: ".." };
                var obj = arg.Children[isRest ? 1 : 0];
                var def = arg.Children.Count > 2 ? arg.Children[^1] : null;
                return new Arg<BishParseTree>(ArgName(obj, i), Default: def, Rest: isRest);
            })
            .ToList()));
        if (args is null) return result;
        if (fixedArgc && args.Any(arg => arg.Default is not null || arg.Rest))
            return result.Error($"Definition of {funcName} should contain no optional or rest argument", body);
        var defaults = args.Select(arg => arg.Default).OfType<BishParseTree>().ToList();
        result.Add(new FuncStart(symbol, args.Select(arg => arg.Name).ToList()), new Inner())
            .Add(args.Select(arg => new Move(arg.Name)).ToList<BishBytecode>());
        foreach (var (arg, i) in defArgs.Enumerate())
        {
            var isRest = arg.Children[0] is { Text: ".." };
            var obj = arg.Children[isRest ? 1 : 0];
            if (!BishScope.Discard(ArgName(obj, i)))
                result.Add(Def(obj, CompileResult.Expr(null).Add(new Del(ArgName(obj, i))))).Add(new Pop());
        }

        result.Add(Visit(expr).Wrap().IntoReturn());
        result.Add(new Outer(), new FuncEnd(symbol));
        foreach (var @default in defaults) result.Add(Visit(@default), StackEffect.Expr);
        var async = rest.Any(t => t is { Text: "async" });
        var gen = rest.Any(t => t is { Text: "*" });
        result.Add(new MakeFunc(symbol, defaults.Count, args.Count != 0 && args[^1].Rest, gen, async));
        foreach (var deco in decos.Reverse()) result.Add(Visit(deco), StackEffect.Expr).Add(new Swap(), new Call(1));
        if (name is not null) result.Add(new Def(name));
        return result;
    }

    public CompileResult EvalAndCopy(BishParseTree? expr) => new CompileResult(StackEffect.Trans, expr)
        .Add(new Inner()).Add(expr is null ? CompileResult.Stat(null) : Visit(expr).Unwrap().IntoStat())
        .Add(new CopyVars(), new Outer());

    public CompileResult TestType(BishParseTree? tree, string tagName, CompileResult type,
        object? var, CompileResult? post = null)
    {
        var result = CompileResult.Pattern(tree);
        var tag = Symbols.Get(tagName);
        result.Add(type, StackEffect.Expr).Add(new TestType(tag));
        // ReSharper disable once InvertIf
        if (var is not null)
        {
            var value = CompileResult.Expr(null).Add(new Del("$_"));
            if (post is not null) result.Add(post, StackEffect.Trans);
            result.Add(new Move("$_")).Add(var switch
            {
                BishParseTree expr => Def(expr, value),
                string id => Def(id, value),
                _ => throw Impossible
            });
        }

        return result.Add(new Pop().Tagged(tag));
    }

    public CompileResult IsErr(BishParseTree tree, object? var) =>
        TestType(tree, "is_err", CompileResult.Expr(null).Add(new GetBuiltin("Error$Result")), var,
            new CompileResult(StackEffect.Trans, null).Add(new GetMember("error")));

    public CompileResult Switch(BishParseTree tree, CompileResult expr,
        IList<(CompileResult pattern, CompileResult codes)> branches)
    {
        var result = CompileResult.Same(tree, branches.Select(pair => pair.codes).ToList());
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

    public CompileResult Multi(List<BishParseTree> trees)
    {
        if (trees.Count == 0) return CompileResult.Stat(null);
        BishParseTree? last = null;
        if (trees[^1] is not { Text: ";" })
        {
            last = trees[^1];
            trees = trees[..^1];
        }

        var expr = last is null ? CompileResult.Stat(null) : Visit(last);
        var result = CompileResult.Same(null, expr);
        foreach (var front in trees.Where(t => t is not { Text: ";" }))
            result.Add(Visit(front).IntoStat());
        return result.Add(expr);
    }

    public CompilerResult<BishParseTree?> ExpandMacros(BishParseTree tree)
    {
        var expands = false;
        List<CompilationError> errors = [];
        if (tree is ("MacroExpr", [_, var macro, _, .. var rest]))
        {
            try
            {
                var expr = rest.ElementAtOrDefault(^2) ?? (BishObject)BishNull.Instance;
                var frame = BishCompileService.Compile("bish", macro);
                frame.Scope = Scope;
                var func = frame.Eval() ?? BishNull.Instance;
                tree = func.Call(new BishArgs([expr], frame)) switch
                {
                    BishNull => BishParseTree.Empty,
                    { } result => result.As<BishParseTree>("macro result")
                };
                expands = true;
            }
            catch (Exception e)
            {
                errors.Add(new CompilationError(SourcePosition.From(tree), e.Message));
            }
        }
        
        foreach (var (child, i) in tree.Children.Enumerate())
        {
            var (expanded, sub) = ExpandMacros(child);
            if (expanded is not null) tree.Children[i] = expanded;
            errors.AddRange(sub);
        }

        return new CompilerResult<BishParseTree?>(expands ? tree : null, errors);
    }

    [SuppressMessage("ReSharper", "TailRecursiveCall")]
    public CompileResult Visit(BishParseTree tree)
    {
        switch (tree)
        {
            case ("IntAtom", [{ Text: { } text }]):
                return CompileResult.Expr(tree).TryAdd(() => new Int(ToInt(text)));
            case ("NumAtom", [{ Text: { } text }]):
                return CompileResult.Expr(tree).TryAdd(() => new Num(ToNum(text)));
            case ("StrAtom", [{ Text: { } text }]):
                return CompileResult.Expr(tree).TryAdd(() => new String(ToStr(text)));
            case ("NullAtom" or "Empty", _): return CompileResult.Expr(tree).Add(new Null());
            case ("BoolAtom", [{ Text: { } text }]):
                return CompileResult.Expr(tree).TryAdd(() => new Bool(text == "true"));
            case ("IdAtom", [var id]): return CompileResult.Expr(tree).Add(new Get(IdName(id)));
            case ("AtomExpr", [var atom]): return Visit(atom);
            case ("ParenExpr", [_, var child, _]): return Visit(child);
            case ("UnOpExpr", [{ Text: { } op }, var expr]):
                return CompileResult.Expr(tree).Add(Visit(expr), StackEffect.Expr)
                    .Add(op == "!" ? new Not() : Op(op, 1));
            case ("BinOpExpr", [var left, { Text: { } op }, var right]):
                return CompileResult.Expr(tree)
                    .Add(Visit(left), StackEffect.Expr)
                    .Add(Visit(right), StackEffect.Expr)
                    .Add(op switch
                    {
                        "===" => [new RefEq()],
                        "!==" => [new RefEq(), new Not()],
                        _ => [Op(op, 2)]
                    });
            case ("SingleIndex", [_, var index, _]):
                return new CompileResult(StackEffect.Expr, tree).Add(Visit(index), StackEffect.Expr);
            case ("RangeIndex", [_, .. var range, _]):
            {
                BishParseTree? step = null;
                if (range.Count(t => t is { Text: ":" }) == 2)
                {
                    step = range[^1];
                    range = range.ToList()[..^2];
                }

                var start = range[0] switch { { Text: ":" } => null, { } t => t };
                var end = range[^1] switch { { Text: ":" } => null, { } t => t };
                return new CompileResult(StackEffect.Expr, tree).Add(new GetBuiltin("range"))
                    .Add(OrNull(start), StackEffect.Expr).Add(OrNull(end), StackEffect.Expr)
                    .Add(OrNull(step), StackEffect.Expr).Add(new Call(3));
            }
            case ("LogicAndExpr", [var left, _, var right]):
            {
                var result = CompileResult.Expr(tree);
                var tag = Symbols.Get("bin_and");
                return result.Add(Visit(left))
                    .Add(new Op("bool", 1), new Copy(), new JumpIfNot(tag), new Pop())
                    .Add(Visit(right))
                    .Add(Tag(tag));
            }
            case ("LogicOrExpr", [var left, _, var right]):
            {
                var result = CompileResult.Expr(tree);
                var tag = Symbols.Get("bin_or");
                return result.Add(Visit(left))
                    .Add(new Op("bool", 1), new Copy(), new JumpIf(tag), new Pop())
                    .Add(Visit(right))
                    .Add(Tag(tag));
            }
            case ("NullCombExpr", [var left, _, var right]):
            {
                var result = CompileResult.Expr(tree);
                var tag = Symbols.Get("null_comb");
                return result.Add(Visit(left))
                    .Add(new Copy(), Op("nullish", 1), new JumpIfNot(tag), new Pop())
                    .Add(Visit(right))
                    .Add(Tag(tag));
            }
            case ("SingleArg", [var expr]): return Visit(expr);
            case ("RestArg", [_, var expr]): return Visit(expr);
            case ("ListExpr", [_, ("Args", var children), _]):
            {
                var args = children.Where(t => t is not { Text: "," }).ToArray();
                if (HasRest(args)) return ToList(args).WithTree(tree);
                var result = CompileResult.Expr(tree);
                foreach (var arg in args) result.Add(Visit(arg), StackEffect.Expr);
                result.Add(new BuildList(args.Length));
                return result;
            }
            case ("MapExpr", [_, var node, _]):
            {
                var result = CompileResult.Expr(tree).Add(new GetBuiltin("map"), new Call(0));
                var entries = node is { Text: ":" } ? [] : node.Children.Where(t => t is not { Text: "," }).ToArray();
                foreach (var entry in entries)
                    switch (entry)
                    {
                        case ("RestEntry", [_, var expr]):
                            result.Add(Visit(expr), StackEffect.Expr);
                            result.Add(Op("+", 2));
                            break;
                        case ("SingleEntry", [var key, _, var value]):
                            result.Add(new Copy());
                            result.Add(Visit(key), StackEffect.Expr);
                            result.Add(Visit(value), StackEffect.Expr);
                            result.Add(Op("def[]", 3), new Pop());
                            break;
                        default: throw Impossible;
                    }

                return result;
            }
            case ("ObjExpr", [_, var node, _]):
            {
                var result = CompileResult.Expr(tree).Add(new GetBuiltin("object"), new Call(0));
                var entries = node is { Text: "." } ? [] : node.Children.Where(t => t is not { Text: "," }).ToArray();
                foreach (var entry in entries.Where(t => t is not { Text: "," }))
                {
                    var id = IdName(entry.Children[1]);
                    var expr = entry.Children.ElementAtOrDefault(3);
                    result.Add(new Copy());
                    if (expr is null) result.Add(new Get(id));
                    else result.Add(Visit(expr), StackEffect.Expr);
                    result.Add(new MoveMember(id));
                }

                return result;
            }
            case ("GetAccess", [.. _, var last]):
            {
                var tag = Symbols.Get("get");
                return CompileResult.Expr(tree).Add(GetExceptLast(tree, tag))
                    .Add(Get(last, tag)).Add(Tag(tag));
            }
            case ("Set", [var obj, .. var some, var value]):
            {
                var op = some[0] is ("SetOp", [var x]) ? x.Text : null;
                return Set(obj, op, Visit(value));
            }
            case ("Def", [var obj, _, var value]): return Def(obj, Visit(value));
            case ("Del", [_, var obj]): return Del(obj);
            case ("IfExpr", [_, _, var cond, _, var left, .. var rest]):
            {
                var right = rest.ElementAtOrDefault(^1);
                return Condition("if", Visit(cond), Visit(left).Wrap(),
                    right is null ? null : Visit(right).Wrap()).WithTree(tree);
            }
            case ("BreakExpr", [_, .. var rest]):
            {
                var tag = rest.Count == 1 ? IdName(rest[0]) : null;
                return CompileResult.Expr(tree).Add(new Break(tree, tag));
            }
            case ("ContinueExpr", [_, .. var rest]):
            {
                var tag = rest.Count == 1 ? IdName(rest[0]) : null;
                return CompileResult.Expr(tree).Add(new Continue(tree, tag));
            }
            case ("WhileExpr", [.. var rest, _, _, var cond, _, var loop]):
            {
                var name = rest is [("Tag", [var id, _])] ? IdName(id) : null;
                var (tag, end) = Symbols.GetPair("while");
                return CompileResult.Expr(tree)
                    .Add(Tag(tag))
                    .Add(Visit(cond), StackEffect.Expr)
                    .Add(new JumpIfNot(end))
                    .Add(Visit(loop).IntoStat().Wrap())
                    .Add(new Jump(tag))
                    .Add(Tag(end))
                    .WrapLoop(end, tag, name)
                    .Add(new Null());
            }
            case ("DoWhileExpr", [.. var rest, _, var loop, _, _, var cond, _]):
            {
                var name = rest is [("Tag", [var id, _])] ? IdName(id) : null;
                var (tag, end) = Symbols.GetPair("do_while");
                var @continue = Symbols.Get("do_while_continue");
                return CompileResult.Expr(tree)
                    .Add(Tag(tag))
                    .Add(Visit(loop).IntoStat().Wrap(), StackEffect.Stat)
                    .Add(Tag(@continue))
                    .Add(Visit(cond), StackEffect.Expr)
                    .Add(new JumpIf(tag), Tag(end))
                    .WrapLoop(end, @continue, name)
                    .Add(new Null());
            }
            case ("ForExpr", [.. var rest, ("ForBody", [_, .. var await, _, var obj, _, var iter, _]), var loop]):
            {
                var name = rest is [("Tag", [var id, _])] ? IdName(id) : null;
                return CompileResult.Expr(tree)
                    .Add(Visit(iter), StackEffect.Expr)
                    .Add(ForIter(tree, new CompileResult(StackEffect.Consume, tree).Add(new Move("$for"))
                        .Add(Def(obj, CompileResult.Expr(null).Add(new Del("$for"))))
                        .Add(new Pop())
                        .Add(Visit(loop).IntoStat()), name, await.Count == 1));
            }
            case ("BlockExpr", [_, .. var children, _]): return Multi(children).Wrap();
            case ("ReturnExpr", [_, .. var rest]):
            {
                var result = CompileResult.Expr(null);
                if (rest is [var expr]) result.Add(Visit(expr), StackEffect.Expr);
                else result.Add(new Null());
                return result.Add(new Ret(), new Null());
            }
            case ("YieldExpr", [_, .. var rest, var expr]):
            {
                var await = rest.Any(t => t is { Text: "await" });
                var gen = rest.Any(t => t is { Text: "*" });
                var result = CompileResult.Expr(tree).Add(Visit(expr), StackEffect.Expr);
                if (gen)
                    result.Add(ForIter(tree, new CompileResult(StackEffect.Consume, null)
                        .Add(new Yield()), null, await));
                else
                {
                    if (await) result.Add(new Await(), new Yield());
                    else result.Add(new Yield());
                    result.Add(new Null());
                }

                return result;
            }
            case ("AwaitExpr", [_, var expr]):
                return CompileResult.Expr(tree).Add(Visit(expr), StackEffect.Expr).Add(new Await());
            case ("Deco", [_, var deco]): return Visit(deco);
            case ("FuncExpr", [.. var rest, var body]):
            {
                var name = rest.LastOrDefault() is (var type, _) id && type.EndsWith("Id") ? IdName(id) : null;
                var decos = rest.Where(t => t is ("Deco", _)).ToArray();
                return MakeFunc(CompileResult.Expr(tree), name, body, decos);
            }
            case ("OperExpr", [.. var deco, _, ("DefOp", var ops), ("FuncBody", [_, ("DefArgs", var args), ..]) body]):
            {
                var op = string.Join("", ops.Select(t => t.Text));
                var result = CompileResult.Expr(tree);
                var special = result.Try(() => BishOperator.GetOperator(op, args.Count(t => t is not { Text: "," })));
                var name = special?.NamePattern.Name;
                return MakeFunc(result, name, body, deco, special?.Args is not null, $"operator {op}");
            }
            case ("AccessExpr", [.. var rest, ("FuncBody", [_, ("DefArgs", var args), ..]) body]):
            {
                var result = CompileResult.Expr(tree);
                var decos = rest.Where(t => t is ("Deco", _)).ToArray();
                rest = rest.Where(t => t is not ("Deco", _)).ToList();
                var op = rest[0].Children[0].Text!;
                var item = rest.ElementAtOrDefault(1);
                var opName = op + item switch
                {
                    null => "()",
                    ("AccessItem", [_, _]) => "[]",
                    ("AccessItem", [_]) => "",
                    _ => throw Impossible
                };
                var special = result.Try(() => BishOperator
                    .GetOperator(opName, args.Count(t => t is not { Text: "," })));
                var access = op + op[^1] + "er";
                var (name, funcName) = item switch
                {
                    null => (special?.NamePattern.Name, access),
                    ("AccessItem", [_, _]) => (special?.NamePattern.Name, $"index {access}"),
                    ("AccessItem", [var id]) => ($"hook_{op}_{IdName(id)}", $"{access} {IdName(id)}"),
                    _ => throw Impossible
                };
                return MakeFunc(result, name, body, decos, true, funcName);
            }
            case ("HookExpr", [.. var deco, var defHook, ("FuncBody", [_, ("DefArgs", var args), ..]) body]):
            {
                var result = CompileResult.Expr(tree);
                var hook = defHook.Children[0].Text!;
                var special = result.Try(() => BishOperator.GetOperator(hook, args.Count(t => t is not { Text: "," })));
                var name = special?.NamePattern.Name;
                return MakeFunc(result, name, body, deco, false, $"hook {hook}");
            }
            case ("ClassExpr", var children):
            {
                var decos = children.Where(t => t is ("Deco", _)).ToArray();
                var rest = children.Where(t => t is not ("Deco", _)).ToList();
                rest = rest[1..];
                BishParseTree? meta = null;
                if (rest.FirstOrDefault() is { Text: "[" })
                {
                    meta = rest[1];
                    rest = rest[3..];
                }

                string? name = null;
                if (rest.FirstOrDefault() is (var type, _) id && type.EndsWith("Id"))
                {
                    name = IdName(id);
                    rest = rest[1..];
                }

                BishParseTree[] args = [];
                if (rest.FirstOrDefault() is { Text: ":" } && rest[1] is ("Args", var c))
                {
                    args = c.Where(t => t is not { Text: "," }).ToArray();
                    rest = rest[2..];
                }

                var body = rest.FirstOrDefault();
                var result = CompileResult.Expr(tree);
                if (meta is not null) result.Add(Visit(meta), StackEffect.Expr);
                else result.Add(new GetBuiltin("type"));
                result.Add(new String(name ?? Anonymous)).Add(ToList(args)).Add(new Call(2)).Add(EvalAndCopy(body));
                foreach (var deco in decos.Reverse())
                    result.Add(Visit(deco), StackEffect.Expr).Add(new Swap(), new Call(1));
                if (name is not null) result.Add(new Def(name));
                return result;
            }
            case ("ExtendExpr", [_, var obj, var body]):
                return CompileResult.Expr(tree).Add(Visit(obj), StackEffect.Expr).Add(EvalAndCopy(body));
            case ("ThrowExpr", [_, var expr]):
                return CompileResult.Expr(tree).Add(Visit(expr), StackEffect.Expr).Add(new Throw());
            case ("TryExpr", [_, var expr]):
            {
                var tag = Symbols.Get("try");
                return CompileResult.Expr(tree).Add(new TryStart(tag)).Add(Visit(expr).IntoExpr()).Add(new TryEnd(tag));
            }
            case ("WithExpr", [("WithBody", [_, .. var rest, var cont, _]), var main]):
            {
                var await = rest[0] is { Text: "await" };
                var obj = rest.Count > 2 ? rest[^2] : null;

                var tag = Symbols.Get("with");
                var name = Symbols.Get("$with");

                var dispose = CompileResult.Stat(tree).Add(new Get(name), Op("dispose", 1));
                if (await) dispose.Add(new Await());
                dispose.Add(new Pop());

                var body = CompileResult.Expr(main);
                foreach (var (code, free) in Visit(main).IntoExpr().GetFrees<Ret>())
                {
                    if (free) body.Add(dispose);
                    body.Add(code);
                }

                var result = CompileResult.Expr(tree).Add(Visit(cont), StackEffect.Expr)
                    .Add(new Move(name), new TryStart(tag));
                if (obj is not null) result.Add(Def(obj, CompileResult.Expr(null).Add(new Get(name)))).Add(new Pop());
                return result.Add(body)
                    .Add(new TryEnd(tag), new Def("$value"))
                    .Add(IsErr(tree, "$err"))
                    .Add(new Move("$isErr"))
                    .Add(dispose)
                    .Add(Condition("try_with",
                        CompileResult.Expr(null).Add(new Del("$isErr")),
                        CompileResult.Stat(null).Add(new Get("$err"), new Throw()),
                        CompileResult.Stat(null).Add(new Del("$value"))))
                    .Wrap();
            }
            case ("NullPattern", _): return CompileResult.Pattern(tree).Add(new GetBuiltin("null")).Add(new RefEq());
            case ("ParenPattern", [_, var pattern, _]): return Visit(pattern);
            case ("ListPattern", [_, .. var children, _]):
            {
                var result = CompileResult.Pattern(tree);
                var items = children.Where(t => t is not { Text: "," }).ToArray();

                if (items.Length == 0) return result.Add(new BuildList(0), Op("==", 2));

                int? rest = null;
                foreach (var (item, i) in items.Enumerate())
                {
                    if (item.Children.Count == 1) continue;
                    if (rest is null) rest = i;
                    else result.Error("Found list deconstruct pattern with multiple rest pattern");
                }

                var end = Symbols.Get("list");
                var tags = Enumerable.Range(0, items.Length).Select(_ => Symbols.Get("list")).ToList();
                result.Add(new Move("$_"), new Get("$_"), new GetBuiltin("list"), new TestType(), new Pop())
                    .Add(new JumpIfNot(tags[^1]), new Get("$_"), new GetMember("length"));
                if (rest is null) result.Add(new Int(items.Length), Op("==", 2));
                else result.Add(new Int(items.Length - 1), Op(">=", 2));
                result.Add(new JumpIfNot(tags[^1]));
                for (var i = items.Length - 1; i >= 0; i--)
                {
                    result.Add(new Get("$_"));
                    switch (rest is null ? -1 : i.CompareTo(rest))
                    {
                        case < 0: result.Add(new Int(i)); break;
                        case 0:
                            result.Add(new GetBuiltin("range"), new Int(rest!.Value), new Get("$_"),
                                    new GetMember("length"))
                                .Add(new Int(items.Length - rest.Value - 1), Op("-", 2), new Int(1),
                                    new Call(3)); break;
                        case > 0: result.Add(new Int(i - items.Length)); break;
                    }

                    result.Add(Op("get[]", 2));
                }

                result.Add(new Del("$_"), new Pop(), new Bool(true), new JumpIfNot(tags[^1]));
                foreach (var (item, i) in items.Enumerate())
                    result.Add(Visit(item.Children[^1]), StackEffect.Pattern).Add(new JumpIfNot(tags[i]));
                result.Add(new Bool(true), new Jump(end));
                foreach (var tag in tags[..^1]) result.Add(new Pop().Tagged(tag));
                result.Add(Tag(tags[^1]), new Bool(false), Tag(end));
                return result;
            }
            case ("MapPattern", [_, var node, _]):
            {
                var result = CompileResult.Pattern(tree);
                var entries = node is { Text: ":" }
                    ? []
                    : node.Children.Where(t => t is not { Text: "," }).ToArray();
                if (entries.SkipLast(1).Any(entry => entry is ("RestPatternEntry", _)))
                    result.Error("Rest entry must be the last one in map deconstruction");
                var (tag, end) = Symbols.GetPair("map");
                result.Add(new Copy(), new GetBuiltin("map"), new TestType(), new Pop(),
                    new JumpIfNot(tag), new GetBuiltin("map"), new Swap(), new Call(1));
                foreach (var entry in entries)
                    switch (entry)
                    {
                        case ("SinglePatternEntry", [var expr, _, var pattern]):
                            var (tryTag, tryEnd) = Symbols.GetPair("try");
                            result.Add(new Copy())
                                .Add(Visit(expr), StackEffect.Expr)
                                .Add(new TryStart(tryTag), Op("del[]", 2), new TryEnd(tryTag))
                                .Add(new Copy()).Add(IsErr(tree, null)).Add(new Not(), new Copy())
                                .Add(new JumpIf(tryEnd), new Swap(), new Pop(), new Swap(), new Pop(), new Swap(),
                                    new Pop())
                                .Add(Tag(tryEnd), new JumpIfNot(tag))
                                .Add(Visit(pattern), StackEffect.Pattern)
                                .Add(new JumpIfNot(tag));
                            break;
                        case ("RestPatternEntry", [_, var pattern]):
                            result.Add(new Copy()).Add(Visit(pattern), StackEffect.Pattern).Add(new JumpIfNot(tag));
                            break;
                    }

                result.Add(new Bool(true), new Jump(end), Tag(tag), new Bool(false), Tag(end), new Swap(), new Pop());
                return result;
            }
            case ("ObjPattern", [_, var node, _]):
            {
                var result = CompileResult.Pattern(tree);
                var entries = node is { Text: "." }
                    ? []
                    : node.Children.Where(t => t is not { Text: "," }).ToArray();
                var (tag, end) = Symbols.GetPair("map");
                foreach (var entry in entries)
                {
                    var (tryTag, tryEnd) = Symbols.GetPair("try");
                    result.Add(new Copy())
                        .Add(new TryStart(tryTag), new GetMember(IdName(entry.Children[1])), new TryEnd(tryTag))
                        .Add(new Copy()).Add(IsErr(tree, null)).Add(new Not(), new Copy())
                        .Add(new JumpIf(tryEnd), new Swap(), new Pop(), new Swap(), new Pop(), Tag(tryEnd))
                        .Add(new JumpIfNot(tag))
                        .Add(Visit(entry.Children[^1]), StackEffect.Pattern).Add(new JumpIfNot(tag));
                }

                return result.Add(new Bool(true), new Jump(end), Tag(tag), new Bool(false), Tag(end), new Swap(),
                    new Pop());
            }
            case ("ExprPattern", [var expr]):
            {
                return expr is ("AtomExpr", [("IdAtom", [var id])]) && BishScope.Discard(IdName(id))
                    ? CompileResult.Pattern(tree).Add(new Pop(), new Bool(true))
                    : CompileResult.Pattern(tree).Add(Visit(expr), StackEffect.Expr).Add(Op("==", 2));
            }
            case ("OpPattern", [var op, var expr]):
                return CompileResult.Pattern(tree)
                    .Add(Visit(expr), StackEffect.Expr).Add(Op(op.Children[0].Text!, 2));
            case ("TypePattern", [_, var type, .. var rest]):
            {
                return TestType(tree, "is_of",
                    type is ("AtomExpr", [("IdAtom", [var id])]) && BishScope.Discard(IdName(id))
                        ? CompileResult.Expr(null).Add(new GetBuiltin("object"))
                        : Visit(type), rest.FirstOrDefault());
            }
            case ("ErrPattern", [_, .. var rest]): return IsErr(tree, rest.FirstOrDefault());
            case ("NotPattern", [_, var pattern]):
                return CompileResult.Pattern(tree).Add(Visit(pattern), StackEffect.Pattern).Add(new Not());
            case ("AndPattern", [var left, _, var right]):
            {
                var (tag, end) = Symbols.GetPair("and");
                return CompileResult.Pattern(tree)
                    .Add(new Copy())
                    .Add(Visit(left), StackEffect.Pattern)
                    .Add(Op("bool", 1), new Copy(), new JumpIf(tag), new Swap(),
                        new Pop(), new Jump(end), Tag(tag), new Pop())
                    .Add(Visit(right), StackEffect.Pattern)
                    .Add(Tag(end));
            }
            case ("OrPattern", [var left, _, var right]):
            {
                var (tag, end) = Symbols.GetPair("or");
                return CompileResult.Pattern(tree)
                    .Add(new Copy())
                    .Add(Visit(left), StackEffect.Pattern)
                    .Add(Op("bool", 1), new Copy(), new JumpIfNot(tag), new Swap(),
                        new Pop(), new Jump(end), Tag(tag), new Pop())
                    .Add(Visit(right), StackEffect.Pattern)
                    .Add(Tag(end));
            }
            case ("WhenPattern", [var pattern, _, var expr]):
            {
                var tag = Symbols.Get("when");
                return CompileResult.Pattern(tree)
                    .Add(Visit(pattern), StackEffect.Pattern)
                    .Add(Op("bool", 1), new Copy(), new JumpIfNot(tag), new Pop())
                    .Add(Visit(expr), StackEffect.Expr)
                    .Add(Tag(tag));
            }
            case ("MatchExpr", [var expr, _, var pattern]):
                return CompileResult.Expr(tree).Add(Visit(expr), StackEffect.Expr)
                    .Add(Visit(pattern), StackEffect.Pattern);
            case ("AsExpr", [var expr, _, var type]):
                return CompileResult.Expr(tree).Add(Visit(expr), StackEffect.Expr)
                    .Add(Visit(type), StackEffect.Expr).Add(new TestType(), new Swap(), new Pop());
            case ("SwitchExpr", [_, var expr, _, .. var cases, _]):
                return Switch(tree, Visit(expr), cases.Where(t => t is not { Text: "," })
                    .Select(branch => (Visit(branch.Children[0]), Visit(branch.Children[^1]).Wrap())).ToList());
            case ("PipeVarExpr", _): return CompileResult.Expr(tree).Add(new Get("$"));
            case ("PipeExpr", [var expr, .. var pipes]):
            {
                var result = new CompileResult(StackEffect.Expr, tree);
                var tag = Symbols.Get("tag");
                result.Add(Visit(expr), StackEffect.Expr);
                foreach (var pipe in pipes)
                {
                    if (pipe.Children.Any(t => t is { Text: "?" }))
                        result.Add(new Copy(), Op("nullish", 1), new JumpIf(tag));
                    result.Add(new Move("$")).Add(Visit(pipe.Children[^1]), StackEffect.Expr);
                }

                return result.Add(Tag(tag)).Wrap();
            }
            case ("Program", [.. var children, _]):
            {
                var result = Multi(children);
                const string name = "main$async";
                if (!result.HasFree<Await>()) return result;
                var expr = CompileResult.Expr(tree)
                    .Add(new FuncStart(name, [])).Add(result.IntoReturn()).Add(new FuncEnd(name))
                    .Add(new MakeFunc(name, IsAsync: true), new Call(0), new GetMember("block"));
                return result.Effect == StackEffect.Stat ? expr.IntoStat() : expr;
            }
            default: return CompileResult.Stat(tree).Error($"Invalid ParseTree: {tree.Repr()}");
        }
    }

    public CompileResult VisitFull(BishParseTree tree, bool optimize)
    {
        var (expanded, errors) = ExpandMacros(tree);
        return Visit(expanded ?? tree).Error(errors).Full(optimize);
    }

    internal static ArgumentException Impossible => new("impossible!");
}

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
public record LoopUnbound(BishParseTree Tree, string Name, string? LoopTag) : Unbound(Tree)
{
    public int Depth;

    public LoopUnbound Deeper()
    {
        Depth++;
        return this;
    }

    public override string ErrorMessage() => $"Found {Name} statement out of loop!";
}

public record Break(BishParseTree Tree, string? LoopTag) : LoopUnbound(Tree, "break", LoopTag);

public record Continue(BishParseTree Tree, string? LoopTag) : LoopUnbound(Tree, "continue", LoopTag);

public static class CompileHelper
{
    extension(CompileResult result)
    {
        internal CompileResult WrapLoop(string @break, string @continue, string? loopTag, bool pops = false)
        {
            result.Wrap();
            result.Codes = result.Codes.SelectMany(code => code switch
            {
                Break x when MatchLoopTag(x.LoopTag, loopTag) =>
                    [..Enumerable.Repeat(new Pop().WithPos(x.Pos), x.Depth), new Jump(@break).WithPos(x.Pos)],
                Continue x when MatchLoopTag(x.LoopTag, loopTag) =>
                    [..Enumerable.Repeat(new Pop().WithPos(x.Pos), x.Depth), new Jump(@continue).WithPos(x.Pos)],
                LoopUnbound x when pops => [x.Deeper()],
                _ => (Codes)([code])
            }).ToList();
            return result;
        }

        internal CompileResult IntoExpr() => result.Effect switch
        {
            StackEffect.Expr => result,
            StackEffect.Stat => CompileResult.Expr(result.Tree).Add(result).Add(new Null()),
            _ => throw BishVisitor.Impossible
        };

        internal CompileResult IntoStat() => result.Effect switch
        {
            StackEffect.Expr => CompileResult.Stat(result.Tree).Add(result).Add(new Pop()),
            StackEffect.Stat => result,
            _ => throw BishVisitor.Impossible
        };

        internal CompileResult IntoReturn() => result.Effect switch
        {
            StackEffect.Expr => CompileResult.Stat(result.Tree).Add(result).Add(new Ret()),
            StackEffect.Stat => result,
            _ => throw BishVisitor.Impossible
        };
    }

    private static bool MatchLoopTag(string? unbound, string? loop) => unbound is null || unbound == loop;
}