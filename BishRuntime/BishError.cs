namespace BishRuntime;

public class BishError(string message) : BishObject
{
    public string Message = message;

    // For now, it only stores the function names, from inner to outer.
    public List<string> StackTrace = [];

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Error");

    [Builtin("hook")]
    public static BishError Create() => new("");

    [Builtin("hook")]
    public static void Init(BishError self, [DefaultNull] BishString? message) => self.Message = message?.Value ?? "";

    public override string ToString()
    {
        return $"[{Type.Name}] {Message}" +
               string.Join("", StackTrace.Select(funcName => $"\n  at function {funcName}"));
    }

    static BishError() => BishBuiltinBinder.Bind<BishError>();

    public static readonly BishType AttributeErrorType = new("AttributeError", [StaticType]);
    public static readonly BishType ArgumentErrorType = new("ArgumentError", [StaticType]);
    public static readonly BishType TypeErrorType = new("TypeError", [StaticType]);
    public static readonly BishType NullErrorType = new("NullError", [StaticType]);
    public static readonly BishType NameErrorType = new("NameError", [StaticType]);
    public static readonly BishType ZeroDivisionErrorType = new("ZeroDivisionError", [StaticType]);

    public static readonly BishType IteratorStopType = new("IteratorStop", [StaticType]);
}