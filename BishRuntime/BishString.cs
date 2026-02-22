namespace BishRuntime;

// TODO: Complete this
public class BishString(string value) : BishObject
{
    public string Value { get; private set; } = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string");


    [Builtin("hook")]
    public static BishString Create() => new("");

    [Builtin("hook")]
    public static BishNull Init(BishString self, [DefaultNull] BishString? other)
    {
        self.Value = other?.Value ?? "";
        return BishNull.Instance;
    }

    public override string ToString() => Value;

    [Builtin("op")]
    public static BishBool Eq(BishString a, BishString b) => new(a.Value == b.Value);

    static BishString() => BishBuiltinBinder.Bind<BishString>();
}