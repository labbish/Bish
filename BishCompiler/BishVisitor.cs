global using Codes = System.Collections.Generic.List<BishBytecode.BishBytecode>;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using BishBytecode;
using BishBytecode.Bytecodes;
using BishRuntime;
using String = BishBytecode.Bytecodes.String;

namespace BishCompiler;

public partial class BishVisitor : BishBaseVisitor<Codes>
{
    public const string Anonymous = "anonymous";
    protected readonly SymbolAllocator Symbols = new();

    public static BishBytecode.BishBytecode Tag(string tag) => new Nop().Tagged(tag);

    public override Codes VisitIntAtom(BishParser.IntAtomContext context) =>
        [new Int(int.Parse(context.INT().GetText()))];

    public override Codes VisitNumAtom(BishParser.NumAtomContext context) =>
        [new Num(double.Parse(context.NUM().GetText()))];

    public override Codes VisitStrAtom(BishParser.StrAtomContext context)
    {
        var str = context.STR().GetText();
        var raw = str.StartsWith('r');
        var text = str.TrimStart('r').Trim('#')[1..^1];
        return [new String(raw ? text : Regex.Unescape(text))];
    }

    public override Codes VisitNullAtom(BishParser.NullAtomContext context) => [new Null()];

    public override Codes VisitIdAtom(BishParser.IdAtomContext context) => [new Get(context.GetText())];

    public override Codes VisitParenExpr(BishParser.ParenExprContext context) => Visit(context.expr());

    private static Op Op(string op, int argc) => new(BishOperator.GetOperatorName(op, argc), argc);

    public override Codes VisitUnOpExpr(BishParser.UnOpExprContext context) =>
        [..Visit(context.expr()), Op(context.op.Text, 1)];

    public override Codes VisitBinOpExpr(BishParser.BinOpExprContext context)
    {
        return
        [
            ..Visit(context.left), ..Visit(context.right),
            ..(Codes)(context.op.Text switch
            {
                "===" => [new RefEq()],
                "!==" => [new RefEq(), new Not()],
                { } op => [Op(op, 2)]
            })
        ];
    }

    public override Codes VisitMemberAccess(BishParser.MemberAccessContext context) =>
        [new GetMember(context.ID().GetText())];

    public override Codes VisitIndexAccess(BishParser.IndexAccessContext context) =>
        [..Visit(context.index()), Op("get[]", 2)];

    public override Codes VisitSet(BishParser.SetContext context)
    {
        var name = context.name.Text;
        var op = context.setOp()?.GetText();
        var tag = Symbols.Get("set");
        return op switch
        {
            null => [..Visit(context.expr()), new Set(name)],
            "&&" =>
            [
                new Get(name), new Copy(), new JumpIfNot(tag), new Pop(),
                ..Visit(context.expr()), new Set(name), Tag(tag)
            ],
            "||" =>
            [
                new Get(name), new Copy(), new JumpIf(tag), new Pop(),
                ..Visit(context.expr()), new Set(name), Tag(tag)
            ],
            "??" =>
            [
                new Get(name), new Copy(), new IsNull(), new JumpIfNot(tag), new Pop(),
                ..Visit(context.expr()), new Set(name), Tag(tag)
            ],
            _ => [new Get(name), ..Visit(context.expr()), Op(op, 2), new Set(name)]
        };
    }

    public override Codes VisitSetMember(BishParser.SetMemberContext context)
    {
        var name = context.name.Text;
        var op = context.setOp()?.GetText();
        var tag = Symbols.Get("set");
        // For a.b @= c, we want `a` to be evaluated only once
        return op switch
        {
            null => [..Visit(context.obj), ..Visit(context.value), new SetMember(name)],
            "&&" =>
            [
                ..Visit(context.obj), new Copy(), new GetMember(name), new Copy(), new JumpIfNot(tag), new Pop(),
                ..Visit(context.value), new SetMember(name), new Null(), new Swap(), Tag(tag), new Swap(), new Pop()
            ],
            "||" =>
            [
                ..Visit(context.obj), new Copy(), new GetMember(name), new Copy(), new JumpIf(tag), new Pop(),
                ..Visit(context.value), new SetMember(name), new Null(), new Swap(), Tag(tag), new Swap(), new Pop()
            ],
            "??" =>
            [
                ..Visit(context.obj), new Copy(), new GetMember(name), new Copy(),
                new IsNull(), new JumpIfNot(tag), new Pop(), ..Visit(context.value),
                new SetMember(name), new Null(), new Swap(), Tag(tag), new Swap(), new Pop()
            ],
            _ =>
            [
                ..Visit(context.obj), new Copy(), new GetMember(name),
                ..Visit(context.value), Op(op, 2), new SetMember(name)
            ]
        };
    }

    public override Codes VisitSetIndex(BishParser.SetIndexContext context)
    {
        var op = context.setOp()?.GetText();
        return
        [
            ..Visit(context.obj),
            op is null ? new Nop() : new Copy(),
            ..Visit(context.index()),
            ..(Codes)(op is null ? [] : [new Copy(), new Swap(2), Op("get[]", 2)]),
            ..Visit(context.value),
            op is null ? new Nop() : Op(op, 2),
            Op("set[]", 3)
        ];
    }

    public override Codes VisitDef(BishParser.DefContext context) =>
        [..Visit(context.expr()), new Def(context.name.Text)];

    public override Codes VisitDel(BishParser.DelContext context) => [new Del(context.name.Text)];

    public override Codes VisitDelMember(BishParser.DelMemberContext context) =>
        [..Visit(context.obj), new DelMember(context.name.Text)];

    public override Codes VisitDelIndex(BishParser.DelIndexContext context) =>
        [..Visit(context.obj), ..Visit(context.index()), Op("del[]", 2)];

    public override Codes VisitSingleIndex(BishParser.SingleIndexContext context) => Visit(context.expr());

    private Codes VisitOrNull(BishParser.ExprContext? context) => context is null ? [new Null()] : Visit(context);

    public override Codes VisitRangeIndex(BishParser.RangeIndexContext context) =>
    [
        ..VisitOrNull(context.start), ..VisitOrNull(context.end), ..VisitOrNull(context.step),
        new Get("range"), new SwapCall(3)
    ];

    public override Codes VisitLogicAndExpr(BishParser.LogicAndExprContext context)
    {
        var tag = Symbols.Get("bin_and");
        return
        [
            ..Visit(context.left), new Op("op_Bool", 1), new Copy(),
            new JumpIfNot(tag), new Pop(), ..Visit(context.right), Tag(tag)
        ];
    }

    public override Codes VisitLogicOrExpr(BishParser.LogicOrExprContext context)
    {
        var tag = Symbols.Get("bin_or");
        return
        [
            ..Visit(context.left), new Op("op_Bool", 1), new Copy(),
            new JumpIf(tag), new Pop(), ..Visit(context.right), Tag(tag)
        ];
    }

    public override Codes VisitNullCombExpr(BishParser.NullCombExprContext context)
    {
        var tag = Symbols.Get("bin_or");
        return
        [
            ..Visit(context.left), new Copy(), new IsNull(),
            new JumpIfNot(tag), new Pop(), ..Visit(context.right), Tag(tag)
        ];
    }

    private Codes Condition(string name, Codes cond, Codes left, Codes right)
    {
        var (tag, end) = Symbols.GetPair(name);
        return Wrap([
            ..cond,
            new JumpIfNot(tag),
            ..left,
            new Jump(end),
            Tag(tag),
            ..right,
            Tag(end)
        ]);
    }

    public override Codes VisitTernOpExpr(BishParser.TernOpExprContext context) =>
        Condition("tern", Visit(context.cond), Visit(context.left), Visit(context.right));

    public override Codes VisitCallAccess(BishParser.CallAccessContext context)
    {
        var args = context.args().arg();
        if (NoRest(args)) return [..args.SelectMany(Visit), new Call(args.Length)];
        return [..ToList(args), new CallArgs()];
    }

    public override Codes VisitListExpr(BishParser.ListExprContext context)
    {
        var args = context.args().arg();
        return NoRest(args) ? [..args.SelectMany(Visit), new BuildList(args.Length)] : ToList(args);
    }

    protected Codes ToList(BishParser.ArgContext[] args) =>
    [
        new BuildList(0),
        ..args.SelectMany(arg => Visit(arg).Concat(arg switch
        {
            BishParser.RestArgContext => [new Op("op_Add", 2)],
            _ => [new Swap(), new GetMember("add"), new SwapCall(1)]
        }))
    ];

    protected static bool NoRest(BishParser.ArgContext[] args) => !args.Any(arg => arg is BishParser.RestArgContext);

    public override Codes VisitGetAccess(BishParser.GetAccessContext context)
    {
        var tag = Symbols.Get("null");
        return
        [
            ..Visit(context.expr()),
            ..context.nullAccess().SelectMany(access => access.op is null
                ? Visit(access)
                : [new Copy(), new IsNull(), new JumpIf(tag), ..Visit(access)]),
            Tag(tag)
        ];
    }

    public override Codes VisitEmptyStat(BishParser.EmptyStatContext context) => [];

    public override Codes VisitExprStat(BishParser.ExprStatContext context) =>
        [..Visit(context.expr()), new EndStat()];

    public override Codes VisitBlockStat(BishParser.BlockStatContext context) =>
        Wrap(context.stat().SelectMany(Visit).ToList());

    private static Codes Wrap(params Codes[] codes) => [new Inner(), ..codes.SelectMany(x => x), new Outer()];

    public override Codes VisitIfStat(BishParser.IfStatContext context) =>
        Condition("if", Visit(context.cond), Wrap(Visit(context.left)),
            context.right is null ? [] : Wrap(Visit(context.right)));

    private Codes Return(BishParser.ExprContext expr) => [..Visit(expr), new Ret()];

    public override Codes VisitReturnStat(BishParser.ReturnStatContext context) => Return(context.expr());

    public override Codes VisitFuncExpr(BishParser.FuncExprContext context)
    {
        var name = context.ID()?.GetText();
        var symbol = Symbols.Get(name ?? Anonymous);
        var defArgs = context.defArgs().defArg() ?? [];
        var args = BishFunc.CheckedArgs<Arg<BishParser.ExprContext>, BishParser.ExprContext>(defArgs.Select(arg =>
            new Arg<BishParser.ExprContext>(arg.name.Text, Default: arg.expr(), Rest: arg.dots is not null)).ToList());
        var defaults = args.Select(arg => arg.Default).OfType<BishParser.ExprContext>().ToList();
        var expr = context.expr();
        var stats = context.stat();
        return
        [
            new FuncStart(symbol, args.Select(arg => arg.Name).ToList()),
            new Inner(),
            ..args.Select(arg => new Move(arg.Name)),
            ..expr is null ? stats.SelectMany(Visit) : Return(expr),
            new Outer(),
            new FuncEnd(symbol),
            ..defaults.SelectMany(Visit),
            new MakeFunc(symbol, defaults.Count, Rest: args.Count != 0 && args[^1].Rest),
            ..context.deco().Reverse().SelectMany(deco => Visit(deco).Concat([new SwapCall(1)])),
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
            ..context.deco().Reverse().SelectMany(deco => Visit(deco).Concat([new SwapCall(1)])),
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

    public override Codes VisitProgram(BishParser.ProgramContext context) =>
        context.stat().SelectMany(Visit).ToList();

    internal abstract record Unbound : BishBytecode.BishBytecode
    {
        public abstract void Error();
        public override void Execute(BishFrame frame) => Error();
    }

    public Codes VisitFull(IParseTree tree, bool optimize)
    {
        var codes = Visit(tree);
        foreach (var code in codes)
            if (code is Unbound unbound)
                unbound.Error();
        return optimize ? BishOptimizer.Optimize(codes) : codes;
    }
}

public class SymbolAllocator
{
    public readonly Dictionary<string, int> Used = [];

    public string Get(string symbol)
    {
        var used = Used.GetValueOrDefault(symbol);
        Used[symbol] = used + 1;
        return symbol + (used == 0 ? "" : $"_{used}");
    }

    public (string, string) GetPair(string symbol) => (Get(symbol), Get(symbol + "_end"));
}