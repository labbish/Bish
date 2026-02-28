using BishRuntime;

namespace BishBytecode;

public class BishScope
{
    public readonly BishScope? Outer;
    public readonly Dictionary<string, BishObject> Vars;

    internal BishScope(BishScope? outer = null, Dictionary<string, BishObject>? vars = null)
    {
        Outer = outer;
        Vars = vars ?? [];
    }

    public BishObject? TryGetVar(string name) => Vars.TryGetValue(name, out var value) ? value : Outer?.TryGetVar(name);

    public BishObject GetVar(string name)
    {
        var tip = Discard(name) ? " (note: vars named _, __, ... will be discarded)" :
            name == "$" ? " (did you mean to use it in pipe expression?)" : "";
        return TryGetVar(name) ?? throw BishException.OfName(name)
            .WithExtraMsg(tip);
    }

    public BishObject DefVar(string name, BishObject value) => Discard(name) ? value : Vars[name] = value;

    public BishObject? TrySetVar(string name, BishObject value) =>
        Discard(name) ? value : Vars.ContainsKey(name) ? Vars[name] = value : Outer?.TrySetVar(name, value);

    public BishObject SetVar(string name, BishObject value) =>
        TrySetVar(name, value) ?? throw BishException.OfName(name).WithExtraMsg(" (did you mean to use `:=`?)");

    private static bool Discard(string name) => name.All(c => c == '_');

    public BishObject? TryDelVar(string name) => Vars.Remove(name, out var value) ? value : null;

    public BishObject DelVar(string name) => TryDelVar(name) ?? throw BishException.OfName(name);

    public BishScope CreateInner() => new(this);

    public static readonly Dictionary<string, BishObject> GlobalVars = [];

    public static BishScope Globals => new(vars: new Dictionary<string, BishObject>(GlobalVars));

    public static BishNull Print([Rest] BishList args)
    {
        Console.Write(string.Join("", args.List.Select(arg =>
            arg.GetMember("toString").Call([]).ExpectToBe<BishString>("toString").Value)));
        Console.Out.Flush();
        return BishNull.Instance;
    }

    public static BishString Input() => new(Console.ReadLine() ?? "");

    static BishScope()
    {
        GlobalVars.Add("object", BishObject.StaticType);
        GlobalVars.Add("type", BishType.StaticType);
        GlobalVars.Add("int", BishInt.StaticType);
        GlobalVars.Add("num", BishNum.StaticType);
        GlobalVars.Add("bool", BishBool.StaticType);
        GlobalVars.Add("string", BishString.StaticType);
        GlobalVars.Add("list", BishList.StaticType);
        GlobalVars.Add("map", BishMap.StaticType);
        GlobalVars.Add("range", BishRange.StaticType);
        GlobalVars.Add("true", new BishBool(true));
        GlobalVars.Add("false", new BishBool(false));
        GlobalVars.Add("null", BishNull.Instance);
        GlobalVars.Add("print", BishBuiltinBinder.Builtin("print", Print));
        GlobalVars.Add("input", BishBuiltinBinder.Builtin("input", Input));
        GlobalVars.Add("Error", BishError.StaticType);
        GlobalVars.Add("AttributeError", BishError.AttributeErrorType);
        GlobalVars.Add("ArgumentError", BishError.ArgumentErrorType);
        GlobalVars.Add("TypeError", BishError.TypeErrorType);
        GlobalVars.Add("NullError", BishError.NullErrorType);
        GlobalVars.Add("NameError", BishError.NameErrorType);
        GlobalVars.Add("ZeroDivisionError", BishError.ZeroDivisionErrorType);
        GlobalVars.Add("IterationStop", BishError.IteratorStopType);
    }
}