using BishUtils;

namespace BishRuntime;

public class BishStackLayer(BishFunc func, IList<BishObject> args)
{
    public BishFunc Func => func;
    public IList<BishObject> Args => args;
    public ICodeSource? Source;
    public SourcePosition? Position;

    public BishStackLayer WithSource(ICodeSource? source, SourcePosition? position)
    {
        Source = source;
        Position = position;
        return this;
    }

    public override string ToString() => $"{Func}, calling with ({string.Join(", ", Args)})" +
                                         (Source is null ? "" : $", at {Source.Filename}, {Position}");
}

public class BishError(string message) : BishObject
{
    public string Message = message;

    // From inner to outer
    public readonly IList<BishStackLayer> StackTrace = new ConcurrentList<BishStackLayer>();
    public IList<BishError> Causes = new ConcurrentList<BishError>();

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

    public static readonly BishType AttributeErrorType = new("AttributeError", [StaticType]);
    public static readonly BishType ArgumentErrorType = new("ArgumentError", [StaticType]);
    public static readonly BishType TypeErrorType = new("TypeError", [StaticType]);
    public static readonly BishType NullErrorType = new("NullError", [StaticType]);
    public static readonly BishType ZeroDivisionErrorType = new("ZeroDivisionError", [StaticType]);
    public static readonly BishType ImportErrorType = new("ImportError", [StaticType]);
    public static readonly BishType CompilationErrorType = new("CompilationError", [StaticType]);
    public static readonly BishType BytecodeParserErrorType = new("BytecodeParserError", [StaticType]);
}