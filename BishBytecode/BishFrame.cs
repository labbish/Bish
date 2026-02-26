using BishRuntime;

namespace BishBytecode;

public class BishFrame(List<BishBytecode> bytecodes, BishScope? scope = null, BishFrame? outer = null)
{
    public BishFrame? Outer => outer;
    public BishScope Scope = scope ?? BishScope.Globals;
    public readonly Stack<BishObject> Stack = new();
    public List<BishBytecode> Bytecodes = bytecodes;
    public int Ip;

    public BishObject? ReturnValue;

    // Used by REPL, to display statement result
    public Action<BishObject>? EndStatHandler;

    public BishObject Execute()
    {
        while (Ip < Bytecodes.Count)
        {
            var bytecode = Bytecodes[Ip++];
            try
            {
                bytecode.Execute(this);
            }
            catch (BishException)
            {
                throw;
            }
            catch (Exception e)
            {
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
    public static List<T> Pop<T>(this Stack<T> stack, int count)
    {
        List<T> list = [];
        for (var i = 0; i < count; i++)
            list.Add(stack.Pop());
        return list.Reversed();
    }

    public static List<T> Reversed<T>(this List<T> list)
    {
        list.Reverse();
        return list;
    }
}