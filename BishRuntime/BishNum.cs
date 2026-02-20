using System.Globalization;

namespace BishRuntime;

public class BishNum(double value) : BishObject
{
    public double Value => value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("num");


    [Builtin("hook")]
    public static BishNum Create() => new(0);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    static BishNum() => BuiltinBinder.Bind<BishNum>();
}