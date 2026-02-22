using BishRuntime;

namespace BishBytecode;

public class BishFrame(List<BishBytecode> bytecodes, BishScope? scope = null, BishFrame? outer = null)
{
    public BishFrame? Outer => outer;
    public BishScope Scope = scope ?? BishScope.Globals();
    public readonly Stack<BishObject> Stack = new();
    public List<BishBytecode> Bytecodes => bytecodes;
    public int Ip;

    public void Execute()
    {
        while (Ip < Bytecodes.Count)
        {
            var bytecode = Bytecodes[Ip++];
            bytecode.Execute(this);
        }
    }

    public int FindTag(string tag) => bytecodes.FindIndex(x => x.Tag == tag);
}

public static class StackHelper
{
    public static List<T> Pop<T>(this Stack<T> stack, int count)
    {
        List<T> list = [];
        for (var i = 0; i < count; i++)
            list.Add(stack.Pop());
        return list;
    }
}