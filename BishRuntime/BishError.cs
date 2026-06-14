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

    public override string ToString() =>
        $"{BishString.CallDebug(Func)}, calling with ({string.Join(", ", Args.Select(BishString.CallDebug))})" +
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
    public static BishError New([DefaultNull] BishString? message) => new(message?.Value ?? "");

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

    protected static BishType CreateError(string name)
    {
        name = name.TrimEnd("Type").ToString();
        var error = new BishType(name, [StaticType]);
        BishBuiltinScope.Instance.Init(name, error);
        return error;
    }

    public static readonly BishType AttributeErrorType = CreateError(nameof(AttributeErrorType));
    public static readonly BishType ArgumentErrorType = CreateError(nameof(ArgumentErrorType));
    public static readonly BishType TypeErrorType = CreateError(nameof(TypeErrorType));
    public static readonly BishType NullErrorType = CreateError(nameof(NullErrorType));
    public static readonly BishType ZeroDivisionErrorType = CreateError(nameof(ZeroDivisionErrorType));
    public static readonly BishType ImportErrorType = CreateError(nameof(ImportErrorType));
    public static readonly BishType CompilationErrorType = CreateError(nameof(CompilationErrorType));
    public static readonly BishType BytecodeParserErrorType = CreateError(nameof(BytecodeParserErrorType));
}