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

    public static bool CallToBool(BishObject obj) =>
        BishOperator.Call("bool", [obj]).ExpectToBe<BishBool>("bool").Value;

    static BishBool() => BishBuiltinBinder.Bind<BishBool>();
}

internal class BishBoolType() : BishType("bool")
{
    private static readonly BishFunc Func = BishBuiltinBinder.Builtin("bool", Inits);
    
    public override BishObject TryCall(List<BishObject> args) => Func.TryCall(args);

    private static BishBool Inits([DefaultNull] BishBool? value) => value ?? BishBool.False;
}