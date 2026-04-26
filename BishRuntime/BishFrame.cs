using BishUtils;

namespace BishRuntime;

public class BishFrame(IList<BishBytecode> bytecodes, BishScope? scope = null, BishFrame? outer = null) : BishObject
{
    public BishFrame? Outer { get; private set; } = outer;
    public BishScope Scope = scope ?? BishScope.Globals;
    public Stack<BishObject> Stack = new();
    public IList<BishBytecode> Bytecodes = bytecodes.ToConcurrentList();
    public int Ip;

    public bool Paused;
    public BishObject? ReturnValue;
    public Action<BishObject>? YieldHandler;
    public Action<BishObject>? AwaitHandler;
    public readonly Stack<ErrorHandler> ErrorHandlers = [];

    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("frame");

    [Builtin("hook")]
    public static BishFrame Create(BishObject _) => new([]);

    [Builtin("hook")]
    public static void Init(BishFrame self, BishList bytecodes,
        [DefaultNull] BishScope? scope, [DefaultNull] BishFrame? outer)
    {
        self.Bytecodes = bytecodes.List.Select(item =>
            BishBytecodeParser.FromObject(item.As<BishBytecodeObject>("bytecode"))).ToList();
        self.Scope = scope ?? BishScope.Globals;
        self.Outer = outer;
    }

    public void AddErrorHandler(int ip, Action<BishError> handler) =>
        ErrorHandlers.Push(new ErrorHandler(Scope, new Stack<BishObject>(Stack.Reverse()), ip, handler));

    public BishObject Execute()
    {
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
                if (!ErrorHandlers.TryPop(out var handler)) throw;
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
    public static BishFrame? Get_outer(BishFrame self) => self.Outer;

    [Builtin("hook")]
    public static BishScope Get_scope(BishFrame self) => self.Scope;

    [Builtin("hook")]
    public static BishList Get_stack(BishFrame self) => new(self.Stack.Reverse().ToList());

    [Builtin("hook")]
    public static BishList Get_bytecodes(BishFrame self) =>
        new(self.Bytecodes.Select(BishBytecodeParser.ToObject).ToList<BishObject>());

    [Builtin("hook")]
    public static BishInt Get_ip(BishFrame self) => BishInt.Of(self.Ip);

    [Builtin]
    public static BishObject Execute(BishFrame self) => self.Execute();
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