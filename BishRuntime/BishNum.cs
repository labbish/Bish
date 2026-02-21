using System.Globalization;

namespace BishRuntime;

public class BishNum(decimal value) : BishObject
{
    public decimal Value => value;
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

    public static implicit operator BishNum(BishInt x) => new(x.Value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    
    [Builtin("op")]
    public static BishBool Eq(BishNum a, BishNum b) => new(a.Value == b.Value);

    static BishNum() => BishBuiltinBinder.Bind<BishNum>();
}