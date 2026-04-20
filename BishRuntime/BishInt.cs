namespace BishRuntime;

public class BishInt : BishObject
{
    private BishInt(int value) => Value = value;

    private static readonly BishInt[] Instances = Enumerable.Range(0, 256).Select(i => new BishInt(i - 127)).ToArray();

    public static BishInt Of(int value) => value is > -128 and <= 128 ? Instances[value + 127] : new BishInt(value);

    public readonly int Value;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new BishIntType();

    [Builtin(special: false)]
    public static BishInt Parse(BishString a) => int.TryParse(a.Value, out var value)
        ? Of(value)
        : throw BishException.OfArgument_Parse(a, StaticType);

    [Builtin("op")]
    public static BishInt Pos(BishInt a) => Of(+a.Value);

    [Builtin("op")]
    public static BishInt Neg(BishInt a) => Of(-a.Value);

    [Builtin("op")]
    public static BishInt Add(BishInt a, BishInt b) => Of(a.Value + b.Value);

    [Builtin("op")]
    public static BishInt Sub(BishInt a, BishInt b) => Of(a.Value - b.Value);

    [Builtin("op")]
    public static BishInt Mul(BishInt a, BishInt b) => Of(a.Value * b.Value);

    [Builtin("op")]
    public static BishInt Mod(BishInt a, BishInt b) =>
        b.Value != 0 ? Of(a.Value % b.Value) : throw BishException.OfZeroDivision();

    [Builtin(special: false)]
    public static BishInt Abs(BishInt a) => Of(Math.Abs(a.Value));

    [Builtin(special: false)]
    public static BishInt Sign(BishInt a) => Of(Math.Sign(a.Value));

    public override string ToString() => Value.ToString();

    [Builtin("op")]
    public static BishBool Eq(BishInt a, BishInt b) => BishBool.Of(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishInt a, BishInt b) => Of(a.Value.CompareTo(b.Value));

    [Builtin]
    public static BishBool Bool(BishInt a) => BishBool.Of(a.Value != 0);

    static BishInt() => BishBuiltinBinder.Bind<BishInt>();
}

internal class BishIntType() : BishType("int", [BishNum.StaticType])
{
    private static readonly BishFunc Func = BishBuiltinBinder.Builtin("int", Inits);
    
    public override BishObject TryCall(List<BishObject> args) => Func.TryCall(args);

    private static BishInt Inits([DefaultNull] BishInt? value) => value ?? BishInt.Of(0);
}