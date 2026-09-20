using System.Globalization;
using BishRuntime.Numerals;

namespace BishRuntime;

public class BishNum(BigNum value) : BishObject
{
    public readonly BigNum Value = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("num");

    [Builtin("hook")]
    public static BishNum New([DefaultNull] BishNum? other) => new(other?.Value ?? 0);

    [Builtin]
    public static BishNum Parse(BishString a) => new(BigNum.Parse(a.Value));

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
    public static BishNum Pow(BishNum a, BishNum b) => new(a.Value.Pow(b.Value));

    [Builtin]
    public static BishNum Sqrt(BishNum a) => new(a.Value.Root(2));

    [Builtin]
    public static BishNum Abs(BishNum a) => new(BigNum.Abs(a.Value));

    [Builtin]
    public static BishInt Sign(BishNum a) => BishInt.Of(a.Value.CompareTo(0));

    [Builtin]
    public static BishInt Floor(BishNum a) => BishInt.Of(a.Value.Floor());

    [Builtin]
    public static BishInt Ceil(BishNum a) => BishInt.Of(a.Value.Ceil());

    [Builtin]
    public static BishInt Round(BishNum a) => BishInt.Of(a.Value.Round());

    [Builtin]
    public static BishNum Sin(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Cos(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Tan(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Asin(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Acos(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Atan(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Ln(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Lg(BishNum a) => throw new NotImplementedException();

    [Builtin]
    public static BishNum Log(BishNum a, BishNum b) => throw new NotImplementedException();

    [Builtin]
    public new static BishString Repr(BishObject self, BishReprContext ctx)
    {
        if (self is not BishNum { Value: var value }) throw BishException.OfArgument("Not a num!"); // Fix for int.base
        var sign = BishBool.CallToBool(ctx.Options.At(new BishString("sign"))) && value > 0 ? "+" : "";
        var format = (ctx.Options.At(new BishString("format")) as BishString)?.Value;
        var precision = ctx.Options.At(new BishString("precision")).ToInt();
        var fmt = format switch { "e" => 'e', "E" => 'E', _ => precision is null ? 'G' : 'F' } + precision?.ToString();
        var result = value.ToString(fmt, CultureInfo.InvariantCulture);
        return new BishString(sign + result);
    }

    [Builtin("op")]
    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public static BishBool Eq(BishNum a, BishNum b) => BishBool.Of(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishNum a, BishNum b) => BishInt.Of(a.Value.CompareTo(b.Value));

    [Builtin]
    public static BishBool Bool(BishNum a) => BishBool.Of(a.Value != 0);

    [Builtin("hook")]
    public static BishNum Get_PI(BishType _) => new(BigNum.Pi);

    [Builtin("hook")]
    public static BishNum Get_E(BishType _) => new(BigNum.E);
}