using BishRuntime;

namespace BishBytecode;

public class BishFrame(List<BishBytecode> bytecodes, BishScope? scope = null, BishFrame? outer = null)
{
    public BishFrame? Outer => outer;
    public BishScope Scope = scope ?? BishScope.Globals();
    public readonly Stack<BishObject> Stack = new();
    public List<BishBytecode> Bytecodes => bytecodes;
    public int Ip;

    public BishObject? ReturnValue;

    public BishObject Execute()
    {
        while (Ip < Bytecodes.Count)
        {
            var bytecode = Bytecodes[Ip++];
            bytecode.Execute(this);
            if (ReturnValue is not null) return ReturnValue;
        }
        return BishNull.Instance;
    }
}

public static class Helper
{
    public static List<T> Pop<T>(this Stack<T> stack, int count)
    {
        List<T> list = [];
        for (var i = 0; i < count; i++)
            list.Add(stack.Pop());
        return list;
    }

    public static List<T> Reversed<T>(this List<T> list)
    {
        list.Reverse();
        return list;
    }
}