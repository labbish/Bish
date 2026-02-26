using Antlr4.Runtime;
using BishBytecode.Bytecodes;
using BishRuntime;

namespace BishCompiler;

public partial class BishVisitor
{
    private Codes Return(BishParser.ExprContext expr) => [..Visit(expr), new Ret()];

    public override Codes VisitReturnStat(BishParser.ReturnStatContext context) => Return(context.expr());

    public override Codes VisitFuncExpr(BishParser.FuncExprContext context) => MakeFunc(context.ID()?.GetText(),
        context.defArgs(), context.funcBody(), context.deco());

    public override Codes VisitOperExpr(BishParser.OperExprContext context)
    {
        var op = context.defOp().GetText();
        var special = BishOperator.GetOperator(op, context.defArgs().defArg().Length);
        var name = special.NamePattern.Name;
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        return MakeFunc(name, context.defArgs(), context.funcBody(), context.deco(),
            special.Args is not null, $"operator {op}");
    }

    public override Codes VisitAccessExpr(BishParser.AccessExprContext context)
    {
        var op = context.accessOp().GetText();
        var item = context.accessItem();
        var special = BishOperator.GetOperator(op + (item, item?.ID()) switch
        {
            (null, _) => "()",
            (_, null) => "[]",
            (_, _) => ""
        }, context.defArgs().defArg().Length);
        var access = op + op[^1] + "er";
        var (name, funcName) = (item, item?.ID()) switch
        {
            (null, _) => (special.NamePattern.Name, access),
            (_, null) => (special.NamePattern.Name, $"index {access}"),
            (_, { } id) => ($"hook_{op}_{id.GetText()}", $"{access} {id.GetText()}")
        };
        return MakeFunc(name, context.defArgs(), context.funcBody(), context.deco(), true, funcName);
    }

    public override Codes VisitInitExpr(BishParser.InitExprContext context) =>
        MakeFunc("hook_init", context.defArgs(), context.funcBody(), context.deco(), true, "initializer");

    public override Codes VisitCreateExpr(BishParser.CreateExprContext context) =>
        MakeFunc("hook_create", context.defArgs(), context.funcBody(), context.deco(), true, "create hook");

    private Codes MakeFunc(string? name, BishParser.DefArgsContext? defArgs, BishParser.FuncBodyContext body,
        BishParser.DecoContext[] decos, bool fixedArgc = false, string funcName = "")
    {
        var symbol = Symbols.Get(name ?? Anonymous);
        var args = BishFunc.CheckedArgs<Arg<BishParser.ExprContext>, BishParser.ExprContext>((defArgs?.defArg() ?? [])
            .Select(arg =>
                new Arg<BishParser.ExprContext>(arg.name.Text, Default: arg.expr(), Rest: arg.dots is not null))
            .ToList());
        if (fixedArgc && args.Any(arg => arg.Default is not null || arg.Rest))
            throw new ArgumentException($"Definition of {funcName} should contain no optional or rest argument");
        var defaults = args.Select(arg => arg.Default).OfType<BishParser.ExprContext>().ToList();
        return
        [
            new FuncStart(symbol, args.Select(arg => arg.Name).ToList()),
            new Inner(),
            ..args.Select(arg => new Move(arg.Name)),
            ..body.expr() is null ? body.stat()?.SelectMany(Visit) ?? [] : Return(body.expr()),
            new Outer(),
            new FuncEnd(symbol),
            ..defaults.SelectMany(Visit),
            new MakeFunc(symbol, defaults.Count, Rest: args.Count != 0 && args[^1].Rest),
            ..decos.Reverse().SelectMany(deco => Visit(deco).Concat([new Swap(), new Call(1)])),
            name is null ? new Nop() : new Def(name)
        ];
    }

    public override Codes VisitClassExpr(BishParser.ClassExprContext context)
    {
        var name = context.ID()?.GetText();
        var symbol = Symbols.Get(name ?? Anonymous);
        var args = context.args()?.arg() ?? [];
        var stats = context.stat() ?? [];
        return
        [
            new ClassStart(symbol),
            ..stats.SelectMany(Visit),
            new ClassEnd(symbol),
            ..(Codes)(NoRest(args)
                ? [..args.SelectMany(Visit), new MakeClass(symbol, args.Length)]
                : [..ToList(args), new MakeClassArgs(symbol)]),
            ..context.deco().Reverse().SelectMany(deco => Visit(deco).Concat([new Swap(), new Call(1)])),
            name is null ? new Nop() : new Def(name)
        ];
    }

    public override Codes VisitThrowExpr(BishParser.ThrowExprContext context) => [..Visit(context.expr()), new Throw()];

    public override Codes VisitErrorStat(BishParser.ErrorStatContext context)
    {
        var tag = Symbols.Get("error");
        Codes tryPart = [new TryStart(tag), ..Visit(context.tryStat), new TryEnd(tag)];
        var @catch = context.catchExpr as ParserRuleContext ?? context.catchStat;
        var id = context.ID()?.GetText();
        Codes catchPart = @catch is null
            ? []
            : [new CatchStart(tag), id is null ? new Pop() : new Move(id), ..Visit(@catch), new CatchEnd(tag)];
        var @finally = context.finallyStat;
        Codes finallyPart = @finally is null ? [] : [new FinallyStart(tag), ..Visit(@finally), new FinallyEnd(tag)];
        return [..tryPart, ..catchPart, ..finallyPart];
    }
}