namespace BishRuntime;

public class BishString(string value) : BishObject
{
    public string Value => value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string");
    
    
    [Builtin("hook")]
    public static BishString Create() => new("");

    public override string ToString()
    {
        return Value;
    }

    static BishString() => BishBuiltinBinder.Bind<BishString>();
}