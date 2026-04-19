using BishBytecode;
using BishBytecode.Bytecodes;
using BishRuntime;

namespace BishCompiler;

public partial class BishVisitor
{
    public const string Anonymous = "anonymous";

    private CompileResult Return(BishParser.ExprContext expr) => CompileResult.Expr(null)
        .Add(Visit(expr), StackEffect.Expr).Add(new Ret());

    public override CompileResult VisitReturnExpr(BishParser.ReturnExprContext context) => Return(context.expr());

    public override CompileResult VisitYieldExpr(BishParser.YieldExprContext context)
    {
        var (tag, end) = Symbols.GetPair("yield");
        var result = CompileResult.Expr(context);
        result.Add(Visit(context.expr()), StackEffect.Expr);
        if (context.gen is not null) result.Add(Op("iter", 1), new ForIter(end).Tagged(tag));
        result.Add(new Yield());
        if (context.gen is not null) result.Add(new Jump(tag), Tag(end));
        result.Add(new Null());
        return result;
    }

    public override CompileResult VisitFuncExpr(BishParser.FuncExprContext context) =>
        MakeFunc(context.ID()?.GetText(), context.funcBody(), context.deco());

    public override CompileResult VisitOperExpr(BishParser.OperExprContext context)
    {
        var op = context.defOp().GetText();
        var special = BishOperator.GetOperator(op, context.funcBody().defArgs().defArg().Length);
        var name = special.NamePattern.Name;
        return MakeFunc(name, context.funcBody(), context.deco(), special.Args is not null, $"operator {op}");
    }

    public override CompileResult VisitAccessExpr(BishParser.AccessExprContext context)
    {
        var op = context.accessOp().GetText();
        var item = context.accessItem();
        var special = BishOperator.GetOperator(op + (item, item?.ID()) switch
        {
            (null, _) => "()",
            (_, null) => "[]",
            (_, _) => ""
        }, context.funcBody().defArgs().defArg().Length);
        var access = op + op[^1] + "er";
        var (name, funcName) = (item, item?.ID()) switch
        {
            (null, _) => (special.NamePattern.Name, access),
            (_, null) => (special.NamePattern.Name, $"index {access}"),
            (_, { } id) => ($"hook_{op}_{id.GetText()}", $"{access} {id.GetText()}")
        };
        return MakeFunc(name, context.funcBody(), context.deco(), true, funcName);
    }

    public override CompileResult VisitInitExpr(BishParser.InitExprContext context) =>
        MakeFunc("hook_init", context.funcBody(), context.deco(), false, "initializer");

    public override CompileResult VisitCreateExpr(BishParser.CreateExprContext context) =>
        MakeFunc("hook_create", context.funcBody(), context.deco(), true, "create hook");

    private CompileResult MakeFunc(string? name, BishParser.FuncBodyContext body, BishParser.DecoContext[] decos,
        bool fixedArgc = false, string funcName = "")
    {
        var result = CompileResult.Expr(body);
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
            .Add(args.Select(arg => new Move(arg.Name)).ToList<BishBytecode.BishBytecode>());
        foreach (var arg in defArgs)
            if (!BishScope.Discard(arg.obj.GetText()))
                result.Add(Def(arg.obj, CompileResult.Expr(null).Add(new Del(arg.obj.GetText())))).Add(new Pop());
        result.Add(Visit(body.expr()).Wrap().IntoReturn());
        result.Add(new Outer(), new FuncEnd(symbol));
        foreach (var @default in defaults) result.Add(Visit(@default), StackEffect.Expr);
        result.Add(new MakeFunc(symbol, defaults.Count, args.Count != 0 && args[^1].Rest, body.gen is not null));
        foreach (var deco in decos.Reverse()) result.Add(Visit(deco), StackEffect.Expr).Add(new Swap(), new Call(1));
        if (name is not null) result.Add(new Def(name));
        return result;
    }

    public override CompileResult VisitClassExpr(BishParser.ClassExprContext context)
    {
        var result = CompileResult.Expr(context);
        var name = context.ID()?.GetText();
        var symbol = Symbols.Get(name ?? Anonymous);
        var args = context.args()?.arg() ?? [];
        result.Add(new ClassStart(symbol));
        result.Add(context.expr() is null ? CompileResult.Stat(null) : Visit(context.expr()).Unwrap().IntoStat());
        result.Add(new ClassEnd(symbol));
        if (HasRest(args)) result.Add(ToList(args)).Add(new MakeClassArgs(symbol));
        else
        {
            foreach (var arg in args) result.Add(Visit(arg), StackEffect.Expr);
            result.Add(new MakeClass(symbol, args.Length));
        }

        foreach (var deco in context.deco().Reverse())
            result.Add(Visit(deco), StackEffect.Expr).Add(new Swap(), new Call(1));
        if (name is not null) result.Add(new Def(name));
        return result;
    }

    public override CompileResult VisitThrowExpr(BishParser.ThrowExprContext context) =>
        CompileResult.Expr(context).Add(Visit(context.expr()), StackEffect.Expr).Add(new Throw());

    public override CompileResult VisitErrorExpr(BishParser.ErrorExprContext context)
    {
        var tag = Symbols.Get("error");
        var tryPart = CompileResult.Stat(context.tryStat).Add(new TryStart(tag))
            .Add(Visit(context.tryStat)).Add(new TryEnd(tag));
        var id = context.ID()?.GetText();
        var when = Symbols.Get("when");
        var catchPart = CompileResult.Stat(context);
        catchPart.Add(new CatchStart(tag));
        if (id is not null) catchPart.Add(new Def(id));
        if (context.when is not null)
            catchPart.Add(Visit(context.when), StackEffect.Expr).Add(new JumpIf(when), new Throw(), Tag(when));
        catchPart.Add(new Pop()).Add(Visit(context.catchStat));
        catchPart.Add(new CatchEnd(tag));
        var finallyPart = CompileResult.Stat(context.finallyStat);
        if (context.FIN() is not null)
            finallyPart.Add(new FinallyStart(tag)).Add(Visit(context.finallyStat)).Add(new FinallyEnd(tag));
        return CompileResult.Stat(context).Add(tryPart).Add(catchPart).Add(finallyPart);
    }

    public override CompileResult VisitWithExpr(BishParser.WithExprContext context)
    {
        var tag = Symbols.Get("with");
        var result = CompileResult.Stat(context)
            .Add(Visit(context.cont), StackEffect.Expr)
            .Add(new WithStart(tag));
        if (context.obj is not null)
            result.Add(new Move("$with")).Add(Def(context.obj,
                new CompileResult(StackEffect.Expr, null).Add(new Del("$with"))));
        return result.Add(new Pop()).Add(Visit(context.main).IntoStat()).Add(new WithEnd(tag));
    }
}