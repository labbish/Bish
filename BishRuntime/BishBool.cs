namespace BishRuntime;

public class BishBool(bool value) : BishObject
{
    public bool Value { get; private set; } = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("bool");


    [Builtin("hook")]
    public static BishBool Create() => new(false);
    
    [Builtin("hook")]
    public static BishNull Init(BishBool self, [DefaultNull] BishBool? other)
    {
        self.Value = other?.Value ?? false;
        return BishNull.Instance;
    }
    
    [Builtin("op")]
    public static BishBool Invert(BishBool b) => new(!b.Value);

    [Builtin("op")]
    public static BishBool Eq(BishBool a, BishBool b) => new(a.Value == b.Value);

    public override string ToString() => Value ? "true" : "false";

    static BishBool() => BishBuiltinBinder.Bind<BishBool>();
}