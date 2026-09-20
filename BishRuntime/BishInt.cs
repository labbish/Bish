using BishRuntime.Numerals;

namespace BishRuntime;

public class BishInt : BishNum
{
    private BishInt(BigInt value) : base(value)
    {
    }

    private static readonly BishInt[] Instances = Enumerable.Range(0, 256).Select(i => new BishInt(i - 127)).ToArray();

    public static BishInt Of(BigInt value) =>
        -128 < value && value <= 128 ? Instances[(int)(value + 127)] : new BishInt(value);

    public new BigInt Value => base.Value.Floor();

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("int", [BishNum.StaticType]);

    [Builtin("hook")]
    public static BishInt New([DefaultNull] BishInt? other) => Of(other?.Value ?? 0);

    [Builtin]
    public static BishInt Parse(BishString a, [DefaultNull] BishInt? radix) =>
        Of(Convert.ToBigInt(a.Value, radix?.Value ?? 10));

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
    public static BishInt Mod(BishInt a, BishInt b) => Of(a.Value % b.Value);

    [Builtin]
    public static BishInt Abs(BishInt a) => Of(BigInt.Abs(a.Value));

    [Builtin]
    public static BishString Repr(BishInt self, BishReprContext ctx)
    {
        var sign = BishBool.CallToBool(ctx.Options.At(new BishString("sign"))) && self.Value > 0 ? "+" : "";
        var format = (ctx.Options.At(new BishString("format")) as BishString)?.Value.FirstOrDefault();
        var result = Convert.ToString(self.Value, format switch { 'b' => 2, 'o' => 8, 'x' or 'X' => 16, _ => 10 });
        return new BishString(sign + (format == 'X' ? result.ToUpperInvariant() : result));
    }

    [Builtin("op")]
    public static BishBool Eq(BishInt a, BishInt b) => BishBool.Of(a.Value == b.Value);

    [Builtin("op")]
    public static BishInt Cmp(BishInt a, BishInt b) => Of(a.Value.CompareTo(b.Value));

    [Builtin]
    public static BishBool Bool(BishInt a) => BishBool.Of(a.Value != 0);
}

public static class BishIntHelper
{
    extension(BishObject? obj)
    {
        internal BigInt? ToInt() => obj switch
        {
            BishInt i => i.Value,
            BishNull or null => null,
            _ => throw BishException.OfType_Argument(obj, BishInt.StaticType)
        };
    }
}