namespace BishRuntime;

public class BishBool : BishObject
{
    private BishBool(bool value) => Value = value;

    public static readonly BishBool True = new(true);
    public static readonly BishBool False = new(false);

    public static BishBool Of(bool value) => value ? True : False;

    public readonly bool Value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("bool");

    [Builtin("hook")]
    public static BishBool New([DefaultNull] BishBool? other) => Of(other?.Value ?? false);

    [Builtin("op")]
    public static BishBool Invert(BishBool a) => Of(!a.Value);

    [Builtin("op")]
    public static BishBool Eq(BishBool a, BishBool b) => Of(a.Value == b.Value);

    [Builtin]
    public static BishString Show(BishBool self) => new(self.Value ? "true" : "false");

    [Builtin]
    public static BishBool Bool(BishBool a) => a;

    public static bool CallToBool(BishObject? obj) =>
        obj is not null && BishOperator.Call("bool", [obj]).As<BishBool>("bool").Value;
}