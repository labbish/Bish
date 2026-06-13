using System.Globalization;

namespace BishRuntime;

public class BishNum(double value) : BishObject
{
    public readonly double Value = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("num");

    [Builtin("hook")]
    public static BishNum New([DefaultNull] BishNum? other) => new(other?.Value ?? 0);

    [Builtin]
    public static BishNum Parse(BishString a) => double.TryParse(a.Value, out var value)
        ? new BishNum(value)
        : throw BishException.OfArgument_Parse(a, StaticType);

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

    [Builtin]
    public static BishNum Sqrt(BishNum a) => new(Math.Sqrt(a.Value));

    [Builtin]
    public static BishNum Abs(BishNum a) => new(Math.Abs(a.Value));

    [Builtin]
    public static BishInt Sign(BishNum a) => BishInt.Of(Math.Sign(a.Value));

    [Builtin]
    public static BishInt Floor(BishNum a) => BishInt.Of((int)Math.Floor(a.Value));

    [Builtin]
    public static BishInt Ceil(BishNum a) => BishInt.Of((int)Math.Ceiling(a.Value));

    [Builtin]
    public static BishInt Round(BishNum a) => BishInt.Of((int)Math.Round(a.Value));

    [Builtin]
    public static BishNum Sin(BishNum a) => new(Math.Sin(a.Value));

    [Builtin]
    public static BishNum Cos(BishNum a) => new(Math.Cos(a.Value));

    [Builtin]
    public static BishNum Tan(BishNum a) => new(Math.Tan(a.Value));

    [Builtin]
    public static BishNum Asin(BishNum a) => new(Math.Asin(a.Value));

    [Builtin]
    public static BishNum Acos(BishNum a) => new(Math.Acos(a.Value));

    [Builtin]
    public static BishNum Atan(BishNum a) => new(Math.Atan(a.Value));

    [Builtin]
    public static BishNum Ln(BishNum a) => new(Math.Log(a.Value));

    [Builtin]
    public static BishNum Lg(BishNum a) => new(Math.Log10(a.Value));

    [Builtin]
    public static BishNum Log(BishNum a, BishNum b) => new(Math.Log(a.Value, b.Value));

    [Builtin]
    public static BishString Repr(BishNum self, BishReprContext _) =>
        new(self.Value.ToString(CultureInfo.InvariantCulture));

    [Builtin("op")]
    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public static BishBool Eq(BishNum a, BishNum b) => BishBool.Of(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishNum a, BishNum b) => BishInt.Of(a.Value.CompareTo(b.Value));

    [Builtin]
    public static BishBool Bool(BishNum a) => BishBool.Of(a.Value != 0);

    static BishNum()
    {
        StaticType.DefMember("PI", new BishNum(Math.PI));
        StaticType.DefMember("E", new BishNum(Math.E));
    }
}