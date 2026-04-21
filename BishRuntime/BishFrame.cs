using BishUtils;

namespace BishRuntime;

public class BishFrame(IList<BishBytecode> bytecodes, BishScope? scope = null, BishFrame? outer = null)
{
    public BishFrame? Outer => outer;
    public BishScope Scope = scope ?? BishScope.Globals;
    public readonly Stack<BishObject> Stack = new();
    public readonly IList<BishBytecode> Bytecodes = bytecodes.ToConcurrentList();
    public int Ip;

    public BishObject? ReturnValue;

    public BishObject Execute()
    {
        while (Ip < Bytecodes.Count)
        {
            var bytecode = Bytecodes[Ip++];
            try
            {
                bytecode.Execute(this);
            }
            catch (Exception e)
            {
                if (e is BishException) throw;
                throw new InvalidOperationException($"An exception occurred while executing {bytecode} at {Ip}.", e);
            }

            if (ReturnValue is not null) return ReturnValue;
        }

        return BishNull.Instance;
    }
}

public static class Helper
{
    // FIFO order
    public static IList<T> Pop<T>(this Stack<T> stack, int count)
    {
        List<T> list = [];
        for (var i = 0; i < count; i++)
            list.Add(stack.Pop());
        return ((IEnumerable<T>)list).Reverse().ToConcurrentList();
    }
}