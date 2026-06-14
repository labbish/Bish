using BishUtils;

namespace BishRuntime;

public class BishFrame(IList<BishBytecode> bytecodes, BishScope? scope = null, BishFrame? caller = null) : BishObject
{
    public BishFrame? Caller = caller;
    public BishFunc? Func = null;
    public BishArgs? Args = null;
    public ICodeSource? Source;

    public BishScope Scope = scope ?? BishScope.Globals;
    public Stack<BishObject> Stack = new();
    public IList<BishBytecode> Bytecodes = bytecodes.ToConcurrentList();
    public int Ip;

    public bool Paused;
    public BishObject? ReturnValue;
    public Action<BishObject>? YieldHandler;
    public Action<BishObject>? AwaitHandler;
    public readonly Stack<ErrorHandler> ErrorHandlers = [];

    public BishBytecode? Current => Bytecodes.ElementAtOrDefault(Ip);

    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("frame");

    public static int RecursionLimit { get; set; } = 100;

    [Builtin("hook")]
    public static BishFrame New(BishList bytecodes, [DefaultNull] BishScope? scope, [DefaultNull] BishFrame? caller) =>
        new(bytecodes.List.Select(item => BishBytecodeParser.FromObject(
            item.As<BishBytecodeObject>("bytecode"))).ToList(), scope ?? BishScope.Globals, caller);

    public BishFrame WithSource(ICodeSource? source)
    {
        Source = source;
        return this;
    }

    public void AddErrorHandler(int ip, Action<BishError> handler) =>
        ErrorHandlers.Push(new ErrorHandler(Scope, new Stack<BishObject>(Stack.Reverse()), ip, handler));

    public BishObject Execute()
    {
        if (GetDepth() > RecursionLimit) throw BishException.OfRecursionLimit();
        while (Ip < Bytecodes.Count)
        {
            if (Paused) return BishNull.Instance;
            var bytecode = Bytecodes[Ip++];
            try
            {
                bytecode.Execute(this);
            }
            catch (BishException e)
            {
                if (!ErrorHandlers.TryPop(out var handler))
                {
                    e.Error.StackTrace ??= GetStackTrace();
                    throw;
                }

                Scope = handler.Scope;
                Stack = handler.Stack;
                Ip = handler.Ip;
                handler.Handler(e.Error);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"An exception occurred while executing {bytecode} at {Ip}.", e);
            }

            ErrorHandlers.PopWhile(item => item.Ip <= Ip);
            if (ReturnValue is not null) return ReturnValue;
        }

        return BishNull.Instance;
    }

    [Builtin("hook")]
    public static BishFrame? Get_caller(BishFrame self) => self.Caller;

    [Builtin("hook")]
    public static BishScope Get_scope(BishFrame self) => self.Scope;

    [Builtin("hook")]
    public static BishList Get_stack(BishFrame self) => new(self.Stack.Reverse().ToList());

    [Builtin("hook")]
    public static BishString? Get_source(BishFrame self) =>
        self.Source is FileSource file ? new BishString(file.Filename) : null;

    [Builtin("hook")]
    public static BishFrameCodes Get_codes(BishFrame self) => new(self);

    [Builtin("hook")]
    public static BishList Get_bytecodes(BishFrame self) =>
        new(self.Bytecodes.Select(BishBytecodeParser.ToObject).ToList<BishObject>());

    [Builtin("hook")]
    public static BishInt Get_ip(BishFrame self) => BishInt.Of(self.Ip);

    [Builtin("hook")]
    public static BishFunc? Get_function(BishFrame self) => self.Func;

    [Builtin("hook")]
    public static BishList? Get_arguments(BishFrame self) =>
        self.Args is { Args: var args } ? new BishList(args) : null;

    [Builtin("hook")]
    public static BishInt Get_recursionLimit(BishObject _) => BishInt.Of(RecursionLimit);

    [Builtin("hook")]
    public static void Set_recursionLimit(BishObject _, BishInt num) => RecursionLimit = num.Value;

    [Builtin]
    public static BishObject Execute(BishFrame self) => self.Execute();

    [Builtin]
    public static BishObject? Eval(BishFrame self)
    {
        self.Execute();
        return self.ReturnValue ?? (self.Stack.TryPeek(out var result) ? result : null);
    }

    public BishFrame Clone() => new BishFrame(Bytecodes, Scope).WithSource(Source);

    [Builtin]
    public static BishFrame Clone(BishFrame self) => self.Clone();

    public BishStackLayer? GetStackLayer()
    {
        if (Func is null || Args is null) return null;
        var layer = new BishStackLayer(Func, Args);
        if (Source is not null) layer.AddSource(Source, Current?.Pos);
        return layer;
    }

    [Builtin("hook")]
    public static BishStackLayer? Get_stackLayer(BishFrame self) => self.GetStackLayer();

    public List<T> CollectOnStack<T>(Func<BishFrame, T> func)
    {
        List<T> result = [];
        var current = this;
        do result.Add(func(current));
        // ReSharper disable once ConstantConditionalAccessQualifier
        while ((current = current?.Caller) is not null);
        return result;
    }

    public ConcurrentList<BishStackLayer> GetStackTrace() => CollectOnStack(frame => frame.GetStackLayer())
        .OfType<BishStackLayer>().ToConcurrentList();

    [Builtin("hook")]
    public static BishList Get_stackTrace(BishFrame self) => new(self.GetStackTrace().ToList<BishObject>());

    public int GetDepth() => CollectOnStack(_ => 0).Count;
    
    [Builtin("hook")]
    public static BishInt Get_depth(BishFrame self) => BishInt.Of(self.GetDepth());
}

public record ErrorHandler(BishScope Scope, Stack<BishObject> Stack, int Ip, Action<BishError> Handler);

public static class Helper
{
    // FIFO order
    extension<T>(Stack<T> stack)
    {
        public IList<T> Pop(int count)
        {
            List<T> list = [];
            for (var i = 0; i < count; i++)
                list.Add(stack.Pop());
            return ((IEnumerable<T>)list).Reverse().ToConcurrentList();
        }

        public void PopWhile(Predicate<T> predicate)
        {
            while (stack.TryPeek(out var item) && predicate(item)) stack.Pop();
        }
    }
}

public class BishFrameCodes(BishFrame frame) : BishObject
{
    public BishFrame Frame => frame;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("frame.codes");

    [Builtin("op")]
    public static BishString? GetIndex(BishFrameCodes self, BishObject x) =>
        GetCodes(self, x) is { } codes ? new BishString(codes) : null;

    public static string? GetCodes(BishFrameCodes self, BishObject x) => x switch
    {
        BishInt index => self.GetCode(BishList.GetIndex(self.Frame.Bytecodes, index)),
        BishRange range => self.GetCode(BishList.GetIndex(self.Frame.Bytecodes, range)),
        _ => throw BishException.OfType_Argument(self, BishInt.StaticType)
    };

    public string? GetCode(params IList<BishBytecode> codes)
    {
        if (Frame.Source?.Code is not { } content) return null;
        if (codes.Count == 0) return content;
        var pos = SourcePosition.Combine(codes.Select(code => code.Pos));
        return pos?.Slice(content);
    }
}