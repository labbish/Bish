using System.Collections.Concurrent;
using BishUtils;

namespace BishRuntime;

public class BishScope : BishObject
{
    public readonly BishScope? Outer;

    protected override IList<BishObject> LookupChain => GetLookupChain().ToConcurrentList<BishObject>();

    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Scope");

    internal BishScope(BishScope? outer = null) => Outer = outer;

    public IEnumerable<BishScope> GetLookupChain()
    {
        yield return this;
        if (Outer is null) yield break;
        foreach (var scope in Outer.GetLookupChain())
            yield return scope;
    }

    protected const BishLookupMode Mode = BishLookupMode.NoHook | BishLookupMode.NoAccessor | BishLookupMode.NoBind;

    public BishObject GetVar(string name) => GetMember(name, mode: Mode);

    public BishObject SetVar(string name, BishObject value) =>
        Discard(name) ? value : SetMember(name, value, mode: Mode);

    public BishObject DefVar(string name, BishObject value) =>
        Discard(name) ? value : DefMember(name, value, mode: Mode);

    public BishObject DelVar(string name) => DelMember(name, mode: Mode);

    public static bool Discard(string name) => name.All(c => c == '_');

    public BishScope CreateInner() => new(this);

    [Builtin("hook")]
    public static BishScope? Get_outer(BishScope self) => self.Outer;

    public static readonly IDictionary<string, BishObject> BuiltinModules =
        new ConcurrentDictionary<string, BishObject>();

    public static BishScope Globals => new(BishBuiltinScope.Instance);

    [Builtin("hook")]
    public static BishScope New() => Globals;

    [Builtin]
    public static BishNull Print([Rest] BishList args)
    {
        Console.Write(string.Join("", args.List.Select(BishString.CallShow)));
        Console.Out.Flush();
        return BishNull.Instance;
    }

    [Builtin]
    public static BishString Input() => new(Console.ReadLine() ?? "");

    [Builtin]
    public static void Exit([DefaultNull] BishInt? code) => Environment.Exit(code?.Value ?? 0);

    [Builtin]
    [PassCaller]
    public static BishFrame? This(BishFrame? caller) => caller;

    [Builtin]
    [PassCaller]
    public static BishObject Import(BishFrame? caller, BishString file) =>
        BishImporter.Import(caller?.Scope.GetVar("meta").As<BishMeta>("meta"), file.Value);
    

    [Builtin]
    public static BishBuiltinScope Builtins() => BishBuiltinScope.Instance;
}

public class BishBuiltinScope : BishScope
{
    public void Init(string name, BishObject obj) => Vars[name] = obj;

    private BishBuiltinScope()
    {
        Init("object", BishObject.StaticType);
        Init("type", BishType.StaticType);
        Init("int", BishInt.StaticType);
        Init("num", BishNum.StaticType);
        Init("bool", BishBool.StaticType);
        Init("string", BishString.StaticType);
        Init("list", BishList.StaticType);
        Init("map", BishMap.StaticType);
        Init("range", BishRange.StaticType);
        Init("true", BishBool.True);
        Init("false", BishBool.False);
        Init("null", BishNull.Instance);
        Init("Func", BishFunc.StaticType);
        Init("Error", BishError.StaticType);
        Init("Iterator", BishIterator.Type);
        Init("AsyncIterator", BishIterator.AsyncType);
        Init("IteratorStop", BishIteratorStop.Instance);
        Init("Error$Result", BishErrorResult.StaticType);
        Init("meta", BishMeta.Builtin);
        Init("Scope", StaticType);
        Init("Frame", BishFrame.StaticType);
        Init("Bytecode", BishBytecodeObject.StaticType);
        Init("Runner", BishTaskRunner.StaticType);
        Init("Task", BishTask.StaticType);
    }

    public static readonly BishBuiltinScope Instance = new();
}