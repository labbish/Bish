namespace BishRuntime;

public class BishError(string message) : BishObject
{
    public string Message = message;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Error");

    [Builtin("hook")]
    public static BishError Create() => new("");

    [Builtin("hook")]
    public static BishObject Init(BishError self, [DefaultNull] BishString? message)
    {
        self.Message = message?.Value ?? "";
        return BishNull.Instance;
    }

    public override string ToString() => Message;

    static BishError() => BishBuiltinBinder.Bind<BishError>();

    public static readonly BishType AttributeErrorType = new("AttributeError", [StaticType]);
    public static readonly BishType ArgumentErrorType = new("ArgumentError", [StaticType]);
    public static readonly BishType TypeErrorType = new("TypeError", [StaticType]);
    public static readonly BishType NullErrorType = new("NullError", [StaticType]);
    public static readonly BishType NameErrorType = new("NameError", [StaticType]);
}