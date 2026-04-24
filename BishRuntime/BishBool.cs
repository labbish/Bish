namespace BishRuntime;

public class BishBool : BishObject
{
    private BishBool(bool value) => Value = value;

    public static readonly BishBool True = new(true);
    public static readonly BishBool False = new(false);

    public static BishBool Of(bool value) => value ? True : False;

    public readonly bool Value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new BishBoolType();

    [Builtin("op")]
    public static BishBool Invert(BishBool a) => Of(!a.Value);

    [Builtin("op")]
    public static BishBool Eq(BishBool a, BishBool b) => Of(a.Value == b.Value);

    public override string ToString() => Value ? "true" : "false";

    [Builtin]
    public static BishBool Bool(BishBool a) => a;

    public static bool CallToBool(BishObject? obj) =>
        obj is not null && BishOperator.Call("bool", [obj]).As<BishBool>("bool").Value;
}

internal class BishBoolType() : BishType("bool")
{
    public override BishBool TryCall(IList<BishObject> args) => args.Count > 1
        ? throw BishException.OfArgument_Count(args.Count, 0, 1)
        : args.FirstOrDefault()?.As<BishBool>("bool() argument") ?? BishBool.False;
}