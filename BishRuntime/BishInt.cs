namespace BishRuntime;

public class BishInt : BishObject
{
    private BishInt(int value) => Value = value;

    private static readonly BishInt[] Instances = Enumerable.Range(0, 256).Select(i => new BishInt(i - 127)).ToArray();

    public static BishInt Of(int value) => value is > -128 and <= 128 ? Instances[value + 127] : new BishInt(value);

    public int Value { get; private set; }

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("int");

    [Builtin("hook")]
    public static BishInt Create(BishObject _) => new(0);

    [Builtin("hook")]
    public static void Init(BishInt self, [DefaultNull] BishInt? other) => self.Value = other?.Value ?? 0;

    [Builtin(special: false)]
    public static BishInt Parse(BishString a) => int.TryParse(a.Value, out var value)
        ? BishInt.Of(value)
        : throw BishException.OfArgument_Parse(a, StaticType);

    [Builtin("op")]
    public static BishInt Pos(BishInt a) => new(+a.Value);

    [Builtin("op")]
    public static BishInt Neg(BishInt a) => new(-a.Value);

    [Builtin("op")]
    public static BishInt Add(BishInt a, BishInt b) => new(a.Value + b.Value);

    [Builtin("op")]
    public static BishInt Sub(BishInt a, BishInt b) => new(a.Value - b.Value);

    [Builtin("op")]
    public static BishInt Mul(BishInt a, BishInt b) => new(a.Value * b.Value);

    [Builtin("op")]
    public static BishNum Div(BishInt a, BishInt b) => new((double)a.Value / b.Value);

    [Builtin("op")]
    public static BishInt Mod(BishInt a, BishInt b) =>
        b.Value != 0 ? BishInt.Of(a.Value % b.Value) : throw BishException.OfZeroDivision();

    [Builtin("op")]
    public static BishNum Pow(BishInt a, BishInt b) => new(Math.Pow(a.Value, b.Value));

    [Builtin(special: false)]
    public static BishInt Abs(BishInt a) => new(Math.Abs(a.Value));

    [Builtin(special: false)]
    public static BishInt Sign(BishInt a) => new(Math.Sign(a.Value));

    public override string ToString() => Value.ToString();

    [Builtin("op")]
    public static BishBool Eq(BishInt a, BishInt b) => BishBool.Of(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishInt a, BishInt b) => new(a.Value.CompareTo(b.Value));

    [Builtin]
    public static BishBool Bool(BishInt a) => BishBool.Of(a.Value != 0);

    static BishInt() => BishBuiltinBinder.Bind<BishInt>();
}