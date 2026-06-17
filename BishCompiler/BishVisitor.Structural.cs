using String = BishRuntime.String;

namespace BishCompiler;

public partial class BishVisitor
{
    public const string Anonymous = "anonymous";

    public override CompileResult VisitReturnExpr(BishParser.ReturnExprContext context)
    {
        var result = CompileResult.Expr(null);
        if (context.expr() is null) result.Add(new Null());
        else result.Add(Visit(context.expr()), StackEffect.Expr);
        return result.Add(new Ret(), new Null());
    }

    public override CompileResult VisitYieldExpr(BishParser.YieldExprContext context)
    {
        var result = CompileResult.Expr(context);
        result.Add(Visit(context.expr()), StackEffect.Expr);
        var await = context.await is not null;
        if (context.gen is not null)
            result.Add(ForIter(context, new CompileResult(StackEffect.Consume, null).Add(new Yield()), null, await));
        else
        {
            if (await) result.Add(new Await(), new Yield());
            else result.Add(new Yield());
            result.Add(new Null());
        }

        return result;
    }

    public override CompileResult VisitAwaitExpr(BishParser.AwaitExprContext context) =>
        CompileResult.Expr(context).Add(Visit(context.expr()), StackEffect.Expr).Add(new Await());

    public override CompileResult VisitFuncExpr(BishParser.FuncExprContext context) =>
        MakeFunc(CompileResult.Expr(context), context.ID()?.GetText(), context.funcBody(), context.deco());

    public override CompileResult VisitOperExpr(BishParser.OperExprContext context)
    {
        var result = CompileResult.Expr(context);
        var op = context.defOp().GetText();
        var special = result.Try(() => BishOperator.GetOperator(op, context.funcBody().defArgs().defArg().Length));
        var name = special?.NamePattern.Name;
        return MakeFunc(result, name, context.funcBody(), context.deco(), special?.Args is not null, $"operator {op}");
    }

    public override CompileResult VisitAccessExpr(BishParser.AccessExprContext context)
    {
        var result = CompileResult.Expr(context);
        var op = context.accessOp().GetText();
        var item = context.accessItem();
        var opName = op + (item, item?.ID()) switch
        {
            (null, _) => "()",
            (_, null) => "[]",
            (_, _) => ""
        };
        var special = result.Try(() => BishOperator.GetOperator(opName, context.funcBody().defArgs().defArg().Length));
        var access = op + op[^1] + "er";
        var (name, funcName) = (item, item?.ID()) switch
        {
            (null, _) => (special?.NamePattern.Name, access),
            (_, null) => (special?.NamePattern.Name, $"index {access}"),
            (_, { } id) => ($"hook_{op}_{id.GetText()}", $"{access} {id.GetText()}")
        };
        return MakeFunc(result, name, context.funcBody(), context.deco(), true, funcName);
    }

    public override CompileResult VisitHookExpr(BishParser.HookExprContext context)
    {
        var result = CompileResult.Expr(context);
        var hook = context.defHook().GetText();
        var special = result.Try(() => BishOperator.GetOperator(hook, context.funcBody().defArgs().defArg().Length));
        var name = special?.NamePattern.Name;
        return MakeFunc(result, name, context.funcBody(), context.deco(), false, $"hook {hook}");
    }

    private CompileResult MakeFunc(CompileResult result, string? name, BishParser.FuncBodyContext body,
        BishParser.DecoContext[] decos, bool fixedArgc = false, string funcName = "")
    {
        var symbol = Symbols.Get(name ?? Anonymous);
        var defArgs = body.defArgs()?.defArg() ?? [];
        var args = result.Try(() => BishFunc.CheckedArgs<Arg<BishParser.ExprContext>, BishParser.ExprContext>(defArgs
            .Select(arg =>
                new Arg<BishParser.ExprContext>(arg.obj.GetText(), Default: arg.def, Rest: arg.dots is not null))
            .ToList()));
        if (args is null) return result;
        if (fixedArgc && args.Any(arg => arg.Default is not null || arg.Rest))
            return result.Error(body.defArgs(),
                $"Definition of {funcName} should contain no optional or rest argument");
        var defaults = args.Select(arg => arg.Default).OfType<BishParser.ExprContext>().ToList();
        result.Add(new FuncStart(symbol, args.Select(arg => arg.Name).ToList()), new Inner())
            .Add(args.Select(arg => new Move(arg.Name)).ToList<BishBytecode>());
        foreach (var arg in defArgs)
            if (!BishScope.Discard(arg.obj.GetText()))
                result.Add(Def(arg.obj, CompileResult.Expr(null).Add(new Del(arg.obj.GetText())))).Add(new Pop());
        result.Add(Visit(body.expr()).Wrap().IntoReturn());
        result.Add(new Outer(), new FuncEnd(symbol));
        foreach (var @default in defaults) result.Add(Visit(@default), StackEffect.Expr);
        result.Add(new MakeFunc(symbol, defaults.Count, args.Count != 0 && args[^1].Rest,
            body.gen is not null, body.async is not null));
        foreach (var deco in decos.Reverse()) result.Add(Visit(deco), StackEffect.Expr).Add(new Swap(), new Call(1));
        if (name is not null) result.Add(new Def(name));
        return result;
    }

    private CompileResult EvalAndCopy(BishParser.ExprContext? expr) => new CompileResult(StackEffect.Trans, expr)
        .Add(new Inner()).Add(expr is null ? CompileResult.Stat(null) : Visit(expr).Unwrap().IntoStat())
        .Add(new CopyVars(), new Outer());

    public override CompileResult VisitClsExpr(BishParser.ClsExprContext context)
    {
        var result = CompileResult.Expr(context);
        var name = context.ID()?.GetText();
        var args = context.args()?.arg() ?? [];
        if (context.meta is { } meta) result.Add(Visit(meta), StackEffect.Expr);
        else result.Add(new GetBuiltin("type"));
        result.Add(new String(name ?? Anonymous)).Add(ToList(args)).Add(new Call(2)).Add(EvalAndCopy(context.body));
        foreach (var deco in context.deco().Reverse())
            result.Add(Visit(deco), StackEffect.Expr).Add(new Swap(), new Call(1));
        if (name is not null) result.Add(new Def(name));
        return result;
    }

    public override CompileResult VisitExtendExpr(BishParser.ExtendExprContext context) =>
        CompileResult.Expr(context).Add(Visit(context.obj), StackEffect.Expr).Add(EvalAndCopy(context.body));

    public override CompileResult VisitThrowExpr(BishParser.ThrowExprContext context) =>
        CompileResult.Expr(context).Add(Visit(context.expr()), StackEffect.Expr).Add(new Throw());

    public override CompileResult VisitTryExpr(BishParser.TryExprContext context)
    {
        var tag = Symbols.Get("try");
        return CompileResult.Expr(context).Add(new TryStart(tag))
            .Add(Visit(context.expr()).IntoExpr()).Add(new TryEnd(tag));
    }

    public override CompileResult VisitWithExpr(BishParser.WithExprContext context)
    {
        var tag = Symbols.Get("with");
        var name = Symbols.Get("$with");

        var dispose = CompileResult.Stat(context).Add(new Get(name), Op("dispose", 1));
        if (context.withBody().AWT() is not null) dispose.Add(new Await());
        dispose.Add(new Pop());

        var body = CompileResult.Expr(context.main);
        foreach (var (code, free) in Visit(context.main).IntoExpr().GetFrees<Ret>())
        {
            if (free) body.Add(dispose);
            body.Add(code);
        }

        var result = CompileResult.Expr(context).Add(Visit(context.withBody().cont), StackEffect.Expr)
            .Add(new Move(name), new TryStart(tag));
        if (context.withBody().obj is { } obj)
            result.Add(Def(obj, CompileResult.Expr(null).Add(new Get(name)))).Add(new Pop());
        return result.Add(body)
            .Add(new TryEnd(tag), new Def("$value"))
            .Add(IsErr(context, "$err"))
            .Add(new Move("$isErr"))
            .Add(dispose)
            .Add(Condition("try_with",
                CompileResult.Expr(null).Add(new Del("$isErr")),
                CompileResult.Stat(null).Add(new Get("$err"), new Throw()),
                CompileResult.Stat(null).Add(new Del("$value"))))
            .Wrap();
    }
}