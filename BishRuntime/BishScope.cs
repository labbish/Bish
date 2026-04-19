namespace BishRuntime;

public class BishScope : BishObject
{
    public readonly BishScope? Outer;

    protected override List<BishObject> LookupChain => GetLookupChain().ToList<BishObject>();

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("scope", [BishNum.StaticType]);

    internal BishScope(BishScope? outer = null, Dictionary<string, BishObject>? vars = null)
    {
        Outer = outer;
        Vars = vars ?? [];
        Vars.Add("this", this);
    }

    private IEnumerable<BishScope> GetLookupChain()
    {
        yield return this;
        if (Outer is null) yield break;
        foreach (var scope in Outer.GetLookupChain())
            yield return scope;
    }

    protected const BishLookupMode Mode = BishLookupMode.NoHook | BishLookupMode.NoAccessor;

    public BishObject? TryGetVar(string name) => TryGetMember(name, mode: Mode);

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

    public static readonly Dictionary<string, BishObject> BuiltinModules = [];

    public static BishScope Globals => new(BishBuiltinScope.Instance);

    public static BishNull Print([Rest] BishList args)
    {
        Console.Write(string.Join("", args.List.Select(BishString.CallToString)));
        Console.Out.Flush();
        return BishNull.Instance;
    }

    public static BishString Input() => new(Console.ReadLine() ?? "");

    static BishScope() => BishBuiltinBinder.Bind<BishScope>();
}

public class BishBuiltinScope : BishScope
{
    private void Init(string name, BishObject obj) => Vars[name] = obj;

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
        Init("print", BishBuiltinBinder.Builtin("print", Print));
        Init("input", BishBuiltinBinder.Builtin("input", Input));
        Init("Func", BishFunc.StaticType);
        Init("Error", BishError.StaticType);
        Init("Iterator", BishIterator.StaticType);
        Init("AttributeError", BishError.AttributeErrorType);
        Init("ArgumentError", BishError.ArgumentErrorType);
        Init("TypeError", BishError.TypeErrorType);
        Init("NullError", BishError.NullErrorType);
        Init("NameError", BishError.NameErrorType);
        Init("ZeroDivisionError", BishError.ZeroDivisionErrorType);
        Init("IterationStop", BishError.IteratorStopType);
    }

    public static readonly BishBuiltinScope Instance = new();
}