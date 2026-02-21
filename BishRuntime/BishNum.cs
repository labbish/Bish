using System.Globalization;

namespace BishRuntime;

public class BishNum(double value) : BishObject
{
    public double Value => value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("num");


    [Builtin("hook")]
    public static BishNum Create() => new(0);

    [Builtin("op")]
    public static BishNum Pos(BishNum a) => new(+a.Value);

    [Builtin("op")]
    public static BishNum Neg(BishNum a) => new(-a.Value);

    [Builtin("op")]
    public static BishNum Add(BishNum a, BishNum b) => new(a.Value + b.Value);

    [Builtin("op")]
    public static BishNum Sub(BishNum a, BishNum b) => new(a.Value - b.Value);

    [Builtin("op")]
    public static BishNum Mul(BishNum a, BishNum b) => new(a.Value * b.Value);

    [Builtin("op")]
    public static BishNum Div(BishNum a, BishNum b) => new(a.Value / b.Value);

    [Builtin("op")]
    public static BishNum Mod(BishNum a, BishNum b) => new(a.Value % b.Value);

    [Builtin("op")]
    public static BishNum Pow(BishNum a, BishNum b) => new(Math.Pow(a.Value, b.Value));

    [Builtin(special: false)]
    public static BishNum Abs(BishNum a) => new(Math.Abs(a.Value));
    
    [Builtin(special: false)]
    public static BishInt Sign(BishNum a) => new(Math.Sign(a.Value));

    [Builtin(special: false)]
    public static BishInt Floor(BishNum a) => new((int)Math.Floor(a.Value));

    [Builtin(special: false)]
    public static BishInt Ceil(BishNum a) => new((int)Math.Ceiling(a.Value));

    [Builtin(special: false)]
    public static BishInt Round(BishNum a) => new((int)Math.Round(a.Value));
    
    // TODO: some more math methods

    public static implicit operator BishNum(BishInt x) => new(x.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    [Builtin("op")]
    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public static BishBool Eq(BishNum a, BishNum b) => new(a.Value == b.Value);
    
    [Builtin("op")]
    public static BishInt Cmp(BishNum a, BishNum b) => new(a.Value.CompareTo(b.Value));

    // TODO: maybe some math consts? (e.g. e, PI)
    static BishNum() => BishBuiltinBinder.Bind<BishNum>();
}