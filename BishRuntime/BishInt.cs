namespace BishRuntime;

public class BishInt : BishNum
{
    private BishInt(int value) : base(value)
    {
    }

    private static readonly BishInt[] Instances = Enumerable.Range(0, 256).Select(i => new BishInt(i - 127)).ToArray();

    public static BishInt Of(int value) => value is > -128 and <= 128 ? Instances[value + 127] : new BishInt(value);

    public new int Value => (int)base.Value;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("int", [BishNum.StaticType]);

    [Builtin("hook")]
    public static BishInt New([DefaultNull] BishInt? other) => Of(other?.Value ?? 0);

    [Builtin]
    public new static BishInt Parse(BishString a) => int.TryParse(a.Value, out var value)
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

    [Builtin]
    public static BishInt Abs(BishInt a) => Of(Math.Abs(a.Value));

    [Builtin]
    public static BishInt Sign(BishInt a) => Of(Math.Sign(a.Value));

    [Builtin]
    public static BishString Show(BishInt self) => new(self.Value.ToString());

    [Builtin("op")]
    public static BishBool Eq(BishInt a, BishInt b) => BishBool.Of(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishInt a, BishInt b) => Of(a.Value.CompareTo(b.Value));

    [Builtin]
    public static BishBool Bool(BishInt a) => BishBool.Of(a.Value != 0);
}