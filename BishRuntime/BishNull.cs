namespace BishRuntime;

public class BishNull : BishObject
{
    private BishNull()
    {
    }

    public static readonly BishNull Instance = new();

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("null");

    public override string ToString()
    {
        return "null";
    }

    // TODO: singleton
    [Builtin("hook")]
    public static BishNull Create() => Instance;

    [Builtin("hook")]
    public static BishObject Get(BishNull self, BishString name) => throw new BishNullAccessException(name.Value);

    [Builtin("hook")]
    public static BishObject Set(BishNull self, BishString name, BishObject _) =>
        throw new BishNullAccessException(name.Value);

    [Builtin("hook")]
    public static BishObject Del(BishNull self, BishString name) => throw new BishNullAccessException(name.Value);

    // TODO: equality?
    static BishNull() => BuiltinBinder.Bind<BishNull>();
}