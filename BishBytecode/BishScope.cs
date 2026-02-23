using BishRuntime;

namespace BishBytecode;

public class BishScope
{
    public readonly BishScope? Outer;
    public readonly Dictionary<string, BishObject> Vars = [];

    internal BishScope(BishScope? outer = null) => Outer = outer;

    public BishObject? TryGetVar(string name) => Vars.TryGetValue(name, out var value) ? value : Outer?.TryGetVar(name);

    public BishObject GetVar(string name) => TryGetVar(name) ?? throw BishException.OfName(name);

    public BishObject DefVar(string name, BishObject value) => Vars[name] = value;

    public BishObject? TrySetVar(string name, BishObject value) =>
        Vars.ContainsKey(name) ? Vars[name] = value : Outer?.TrySetVar(name, value);

    public BishObject SetVar(string name, BishObject value) =>
        TrySetVar(name, value) ?? throw BishException.OfName(name);

    public BishObject? TryDelVar(string name) => Vars.Remove(name, out var value) ? value : null;

    public BishObject DelVar(string name) => TryDelVar(name) ?? throw BishException.OfName(name);

    public BishScope CreateInner() => new(this);

    public static BishScope Globals() => new()
    {
        Vars =
        {
            ["object"] = BishObject.StaticType,
            ["type"] = BishType.StaticType,
            ["int"] = BishInt.StaticType,
            ["num"] = BishNum.StaticType,
            ["bool"] = BishBool.StaticType,
            ["string"] = BishBool.StaticType,
            ["list"] = BishList.StaticType,
            ["range"] = BishRange.StaticType,
            ["true"] = new BishBool(true),
            ["false"] = new BishBool(false),
            ["null"] = BishNull.Instance, // TODO: maybe make these const
            ["Error"] = BishError.StaticType,
            ["AttributeError"] = BishError.AttributeErrorType,
            ["ArgumentError"] = BishError.ArgumentErrorType,
            ["TypeError"] = BishError.TypeErrorType,
            ["NullError"] = BishError.NullErrorType,
            ["NameError"] = BishError.NameErrorType,
            ["ZeroDivisionError"] = BishError.ZeroDivisionErrorType,
            ["IterationStop"] = BishError.IteratorStopType
        }
    };
}