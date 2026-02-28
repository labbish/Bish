namespace BishRuntime;

public class BishStackLayer(BishFunc func, List<BishObject> args)
{
    public BishFunc Func => func;
    public List<BishObject> Args => args;

    public override string ToString() => $"{Func}, calling with ({string.Join(", ", Args)})";
}

public class BishError(string message) : BishObject
{
    public string Message = message;

    // From inner to outer
    public readonly List<BishStackLayer> StackTrace = [];
    public List<BishError> Causes = [];

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Error");

    [Builtin("hook")]
    public static BishError Create(BishObject _) => new("");

    [Builtin("hook")]
    public static void Init(BishError self, [DefaultNull] BishString? message) => self.Message = message?.Value ?? "";
    
    [Builtin("hook")]
    public static BishString Get_message(BishError self) => new(self.Message);

    [Builtin("hook")]
    public static BishString Set_message(BishError self, BishString msg)
    {
        self.Message = msg.Value;
        return msg;
    }

    public override string ToString()
    {
        var cause = Causes.Count == 0
            ? ""
            : "\nCaused by: " + string.Join("",
                Causes.Select(cause =>
                    "\n" + string.Join("\n", cause.ToString().Split("\n").Select(line => "  " + line))));
        return $"[{Type.Name}] {Message}" +
               string.Join("", StackTrace.Select(funcName => $"\n  at {funcName}")) + cause;
    }

    static BishError() => BishBuiltinBinder.Bind<BishError>();

    public static readonly BishType AttributeErrorType = new("AttributeError", [StaticType]);
    public static readonly BishType ArgumentErrorType = new("ArgumentError", [StaticType]);
    public static readonly BishType TypeErrorType = new("TypeError", [StaticType]);
    public static readonly BishType NullErrorType = new("NullError", [StaticType]);
    public static readonly BishType NameErrorType = new("NameError", [StaticType]);
    public static readonly BishType ZeroDivisionErrorType = new("ZeroDivisionError", [StaticType]);
    public static readonly BishType ImportErrorType = new("NameError", [StaticType]);

    public static readonly BishType IteratorStopType = new("IteratorStop", [StaticType]);
    public static readonly BishType YieldValueType = new("YieldValue", [StaticType]);
}