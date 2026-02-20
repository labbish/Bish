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
    public static BishNum Add(BishNum a, BishNum b) => new(a.Value + b.Value);

    [Builtin("op")]
    public static BishNum Sub(BishNum a, BishNum b) => new(a.Value - b.Value);

    [Builtin("op")]
    public static BishNum Mul(BishNum a, BishNum b) => new(a.Value * b.Value);

    [Builtin("op")]
    public static BishNum Div(BishNum a, BishNum b) => new(a.Value / b.Value);

    public static implicit operator BishNum(BishInt x) => new(x.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    static BishNum() => BishBuiltinBinder.Bind<BishNum>();
}