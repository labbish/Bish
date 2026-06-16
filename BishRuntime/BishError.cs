using BishUtils;

namespace BishRuntime;

public class BishStackLayer(BishFunc func, BishArgs args) : BishObject
{
    public BishFunc Func => func;
    public BishArgs Args => args;
    public ICodeSource? Source;
    public SourcePosition? Position;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("StackLayer");

    public void AddSource(ICodeSource? source, SourcePosition? position)
    {
        Source = source;
        Position = position;
    }

    public override string ToString() =>
        $"{BishString.CallDebug(Func)}, with args ({string.Join(", ", Args.Args.Select(BishString.CallDebug))})" +
        (Source is null ? "" : $", at {Source.Filename}, {Position}");

    [Builtin]
    public static BishString Repr(BishStackLayer self, BishReprContext _) => new(self.ToString());

    [Builtin("hook")]
    public static BishFunc Get_function(BishStackLayer self) => self.Func;

    [Builtin("hook")]
    public static BishList Get_arguments(BishStackLayer self) => new(self.Args.Args);

    [Builtin("hook")]
    public static BishString? Get_sourceFile(BishStackLayer self) =>
        self.Source is { } source ? new BishString(source.Filename) : null;

    [Builtin("hook")]
    public static BishList? Get_sourcePos(BishStackLayer self) => self.Position is { } pos ? pos.ToObject() : null;
}

public class BishError(string message) : BishObject
{
    public string Message = message;

    // From inner to outer
    public IList<BishStackLayer>? StackTrace;
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
        var trace = StackTrace is null
            ? ""
            : string.Join("", StackTrace.Select(funcName => $"\n  at {funcName}"));
        return $"[{Type.Name}] {Message}" + trace + cause;
    }

    [Builtin]
    public static BishString Repr(BishError self, BishReprContext _) => new(self.ToString());

    protected static BishType CreateError(string name)
    {
        name = name.RemoveEnd("Type");
        var error = new BishType(name, [StaticType]);
        BishBuiltinScope.Instance.Init(name, error);
        return error;
    }

    public static readonly BishType AttributeErrorType = CreateError(nameof(AttributeErrorType));
    public static readonly BishType ArgumentErrorType = CreateError(nameof(ArgumentErrorType));
    public static readonly BishType TypeErrorType = CreateError(nameof(TypeErrorType));
    public static readonly BishType NullErrorType = CreateError(nameof(NullErrorType));
    public static readonly BishType ZeroDivisionErrorType = CreateError(nameof(ZeroDivisionErrorType));
    public static readonly BishType RecursionLimitErrorType = CreateError(nameof(RecursionLimitErrorType));
    public static readonly BishType ImportErrorType = CreateError(nameof(ImportErrorType));
    public static readonly BishType CompilationErrorType = CreateError(nameof(CompilationErrorType));
    public static readonly BishType BytecodeParserErrorType = CreateError(nameof(BytecodeParserErrorType));
}