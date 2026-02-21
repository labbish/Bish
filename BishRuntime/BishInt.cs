namespace BishRuntime;

public class BishInt(int value) : BishObject
{
    public int Value => value;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("int");

    [Builtin("hook")]
    public static BishInt Create() => new(0);

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
    public static BishInt Mod(BishInt a, BishInt b) => new(a.Value % b.Value);

    [Builtin("op")]
    public static BishNum Pow(BishInt a, BishInt b) => new(Math.Pow(a.Value, b.Value));

    [Builtin(special: false)]
    public static BishInt Abs(BishInt a) => new(Math.Abs(a.Value));

    [Builtin(special: false)]
    public static BishInt Sign(BishInt a) => new(Math.Sign(a.Value));

    public override string ToString() => Value.ToString();

    [Builtin("op")]
    public static BishBool Eq(BishInt a, BishInt b) => new(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishInt a, BishInt b) => new(a.Value.CompareTo(b.Value));

    static BishInt() => BishBuiltinBinder.Bind<BishInt>();
}