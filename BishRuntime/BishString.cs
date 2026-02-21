namespace BishRuntime;

public class BishString(string value) : BishObject
{
    public string Value => value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string");
    
    
    [Builtin("hook")]
    public static BishString Create() => new("");

    public override string ToString() => Value;
    
    [Builtin("op")]
    public static BishBool Eq(BishString a, BishString b) => new(a.Value == b.Value);

    static BishString() => BishBuiltinBinder.Bind<BishString>();
}