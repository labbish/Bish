namespace BishRuntime;

public class BishBool(bool value) : BishObject
{
    public bool Value { get; private set; } = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("bool");


    [Builtin("hook")]
    public static BishBool Create(BishObject _) => new(false);

    [Builtin("hook")]
    public static void Init(BishBool self, [DefaultNull] BishBool? other) => self.Value = other?.Value ?? false;

    [Builtin("op")]
    public static BishBool Invert(BishBool a) => new(!a.Value);

    [Builtin("op")]
    public static BishBool Eq(BishBool a, BishBool b) => new(a.Value == b.Value);

    public override string ToString() => Value ? "true" : "false";

    [Builtin]
    public static BishBool Bool(BishBool a) => new(a.Value);

    public static bool CallToBool(BishObject obj) =>
        BishOperator.Call("bool", [obj]).ExpectToBe<BishBool>("bool").Value;

    static BishBool() => BishBuiltinBinder.Bind<BishBool>();
}