using System.Diagnostics.CodeAnalysis;
using BishRuntime;

namespace BishBytecode.Bytecodes;

public record Nop : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
    }
}

public record Pop : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Pop();
}

public record Empty : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Clear();
}

public record Int(int Value) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishInt(Value));
}

public record Num(double Value) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishNum(Value));
}

public record String(string Value) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishString(Value));
}

public record Get(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.GetVar(Name));
}

public record Def(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.DefVar(Name, frame.Stack.Pop());
}

public record Set(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.SetVar(Name, frame.Stack.Pop());
}

public record Del(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.DelVar(Name);
}

public record GetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Pop().GetMember(Name));
}

public record SetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var obj = frame.Stack.Pop();
        var value = frame.Stack.Pop();
        obj.SetMember(Name, value);
    }
}

public record DelMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Pop().DelMember(Name);
}

public record Call(int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var func = frame.Stack.Pop();
        var args = frame.Stack.Pop(Argc).Reversed();
        frame.Stack.Push(func.Call(args));
    }
}

public record Op(string Operator, int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop(Argc).Reversed();
        frame.Stack.Push(BishOperator.Call(Operator, args));
    }
}

public record Inner : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope = frame.Scope.CreateInner();
}

public record Outer : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        frame.Scope = frame.Scope.Outer ?? throw new ArgumentException("No outer scope");
}

public record Jump(string GoalTag) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var pos = frame.Bytecodes.FindIndex(x => x.Tag == GoalTag);
        if (pos == -1) throw new ArgumentException($"No such tag: {GoalTag}");
        frame.Ip = pos;
    }
}

public record JumpIf(string GoalTag) : Jump(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var result = frame.Stack.Pop();
        if (BishOperator.Call("op_Bool", [result]).ExpectToBe<BishBool>("condition").Value) base.Execute(frame);
    }
}

public record JumpIfNot(string GoalTag) : Jump(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var result = frame.Stack.Pop();
        if (!BishOperator.Call("op_Bool", [result]).ExpectToBe<BishBool>("condition").Value) base.Execute(frame);
    }
}

public record StartTag<TEnd>(string Name) : BishBytecode where TEnd : EndTag
{
    public override void Execute(BishFrame frame)
    {
        frame.Ip = EndPos(frame, frame.Ip);
    }

    public int EndPos(BishFrame frame, int pos)
    {
        var end = frame.Bytecodes.FindIndex(pos, x => x is TEnd end && end.Name == Name);
        return end == -1 ? throw new ArgumentException($"Start tag without ending: {Name}") : end;
    }
}

public record EndTag(string Name) : Nop;

public abstract record TagBased<TStart, TEnd>(string Name)
    : BishBytecode where TStart : StartTag<TEnd> where TEnd : EndTag
{
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    public record CodeSlice(int StartPos, TStart Start, int EndPos, TEnd End, List<BishBytecode> Code);

    protected CodeSlice Slice(BishFrame frame)
    {
        var startPos = frame.Bytecodes.FindIndex(x => x is TStart start && start.Name == Name);
        if (startPos == -1) throw new ArgumentException($"Start tag named {Name} not found");
        var start = (TStart)frame.Bytecodes[startPos];
        var endPos = start.EndPos(frame, startPos);
        var end = (TEnd)frame.Bytecodes[endPos];
        var code = frame.Bytecodes[(startPos + 1)..endPos];
        return new CodeSlice(startPos, start, endPos, end, code);
    }
}

public record FuncStart(string Name, List<string> Args) : StartTag<FuncEnd>(Name);

public record FuncEnd(string Name) : EndTag(Name);

public record MakeFunc(string Name, int DefaultArgc = 0) : TagBased<FuncStart, FuncEnd>(Name)
{
    public override void Execute(BishFrame frame)
    {
        var slice = Slice(frame);
        var scope = frame.Scope;
        var defaults = frame.Stack.Pop(DefaultArgc);
        frame.Stack.Push(new BishFunc(
            slice.Start.Args.Reversed().Select((arg, i) => new BishArg(arg, null, defaults.ElementAtOrDefault(i)))
                .ToList().Reversed(),
            args =>
            {
                var inner = new BishFrame(slice.Code, scope, frame);
                // The first argument is in the top
                foreach (var arg in args.Reversed())
                    inner.Stack.Push(arg);
                return inner.Execute();
            }));
    }
}

public record Ret : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.ReturnValue = frame.Stack.Pop();
}

public record Copy : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Peek());
}

public record Swap : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var first = frame.Stack.Pop();
        var second = frame.Stack.Pop();
        frame.Stack.Push(first);
        frame.Stack.Push(second);
    }
}

public record ClassStart(string Name) : StartTag<ClassEnd>(Name);

public record ClassEnd(string Name) : EndTag(Name);

public record MakeClass(string Name, int ParentCount = 0) : TagBased<ClassStart, ClassEnd>(Name)
{
    public override void Execute(BishFrame frame)
    {
        var slice = Slice(frame);
        var scope = new BishScope(frame.Scope.CreateInner());
        var inner = new BishFrame(slice.Code, scope, frame);
        inner.Execute();
        var parents = frame.Stack.Pop(ParentCount).Reversed()
            .Select(obj => obj.ExpectToBe<BishType>("parent class")).ToList();
        var type = new BishType(Name, parents);
        foreach (var (key, value) in scope.Vars)
            type.SetMember(key, value);
        frame.Stack.Push(type);
    }
}