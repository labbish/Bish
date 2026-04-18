using BishLib;
using BishRuntime;

namespace BishBytecode;

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

    private const BishLookupMode Mode = BishLookupMode.NoHook | BishLookupMode.NoAccessor;

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

    public static readonly Dictionary<string, BishObject> GlobalVars = [];

    public static BishScope Globals => new(vars: new Dictionary<string, BishObject>(GlobalVars));

    public static readonly Dictionary<string, BishObject> BuiltinModules = [];

    public static BishNull Print([Rest] BishList args)
    {
        Console.Write(string.Join("", args.List.Select(BishString.CallToString)));
        Console.Out.Flush();
        return BishNull.Instance;
    }

    public static BishString Input() => new(Console.ReadLine() ?? "");

    static BishScope()
    {
        BishBuiltinBinder.Bind<BishScope>();

        GlobalVars.Add("object", BishObject.StaticType);
        GlobalVars.Add("type", BishType.StaticType);
        GlobalVars.Add("int", BishInt.StaticType);
        GlobalVars.Add("num", BishNum.StaticType);
        GlobalVars.Add("bool", BishBool.StaticType);
        GlobalVars.Add("string", BishString.StaticType);
        GlobalVars.Add("list", BishList.StaticType);
        GlobalVars.Add("map", BishMap.StaticType);
        GlobalVars.Add("range", BishRange.StaticType);
        GlobalVars.Add("true", BishBool.True);
        GlobalVars.Add("false", BishBool.False);
        GlobalVars.Add("null", BishNull.Instance);
        GlobalVars.Add("print", BishBuiltinBinder.Builtin("print", Print));
        GlobalVars.Add("input", BishBuiltinBinder.Builtin("input", Input));
        GlobalVars.Add("Error", BishError.StaticType);
        GlobalVars.Add("Iterator", BishIterator.StaticType);
        GlobalVars.Add("AttributeError", BishError.AttributeErrorType);
        GlobalVars.Add("ArgumentError", BishError.ArgumentErrorType);
        GlobalVars.Add("TypeError", BishError.TypeErrorType);
        GlobalVars.Add("NullError", BishError.NullErrorType);
        GlobalVars.Add("NameError", BishError.NameErrorType);
        GlobalVars.Add("ZeroDivisionError", BishError.ZeroDivisionErrorType);
        GlobalVars.Add("IterationStop", BishError.IteratorStopType);

        BuiltinModules.Add("thread", BishThreadModule.Module);
        BuiltinModules.Add("file", BishFileModule.Module);
        BuiltinModules.Add("random", BishRandomModule.Module);
        BuiltinModules.Add("func", BishFuncModule.Module);
        // TODO: time
    }
}