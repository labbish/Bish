namespace BishRuntime;

public class BishBool(bool value) : BishObject
{
    public bool Value => value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("bool");


    [Builtin("hook")]
    public static BishBool Create() => new(false);

    public override string ToString() => Value ? "true" : "false";

    static BishBool() => BishBuiltinBinder.Bind<BishBool>();
}