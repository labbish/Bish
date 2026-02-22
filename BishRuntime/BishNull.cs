namespace BishRuntime;

public class BishNull : BishObject
{
    internal BishNull()
    {
    }

    public static readonly BishNull Instance = new();

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("null");

    public override string ToString()
    {
        return "null";
    }

    [Builtin("hook")]
    public static BishNull Create() => Instance;

    [Builtin("hook")]
    public static BishObject Get(BishNull self, BishString name) =>
        throw BishException.OfNull("get", name.Value);

    [Builtin("hook")]
    public static BishObject Set(BishNull self, BishString name, BishObject _) =>
        throw BishException.OfNull("set", name.Value);

    [Builtin("hook")]
    public static BishObject Del(BishNull self, BishString name) =>
        throw BishException.OfNull("delete", name.Value);

    [Builtin("op")]
    public static BishBool Bool(BishNull a) => new(false);

    static BishNull() => BishBuiltinBinder.Bind<BishNull>();
}