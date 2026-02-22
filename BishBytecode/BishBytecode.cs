using BishRuntime;

namespace BishBytecode;

public abstract record BishBytecode
{
    public string? Tag;

    public abstract void Execute(BishFrame frame);

    public BishBytecode Tagged(string tag)
    {
        Tag = tag;
        return this;
    }
}

public record BishBytecodeNop : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
    }
}

public record BishBytecodePop : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Pop();
}

public record BishBytecodeInt(int Value) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishInt(Value));
}

public record BishBytecodeNum(double Value) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishNum(Value));
}

public record BishBytecodeString(string Value) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishString(Value));
}

public record BishBytecodeGet(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.GetVar(Name));
}

public record BishBytecodeDef(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.DefVar(Name, frame.Stack.Pop());
}

public record BishBytecodeSet(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.SetVar(Name, frame.Stack.Pop());
}

public record BishBytecodeDel(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.DelVar(Name);
}

public record BishBytecodeGetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Pop().GetMember(Name));
}

public record BishBytecodeSetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var obj = frame.Stack.Pop();
        var value = frame.Stack.Pop();
        obj.SetMember(Name, value);
    }
}

public record BishBytecodeDelMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Pop().DelMember(Name);
}

// The args should be pushed in reversed order
public record BishBytecodeCall(int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop(Argc);
        var func = frame.Stack.Pop();
        frame.Stack.Push(func.Call(args));
    }
}

public record BishBytecodeInner : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope = frame.Scope.CreateInner();
}

public record BishBytecodeOuter : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        frame.Scope = frame.Scope.Outer ?? throw new ArgumentException("No outer scope");
}

public record BishBytecodeJump(string GoalTag) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var pos = frame.FindTag(GoalTag);
        if (pos == -1) throw new ArgumentException($"No such tag: {GoalTag}");
        frame.Ip = pos;
    }
}

public record BishBytecodeJumpIf(string GoalTag, bool Reverse = false) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var result = frame.Stack.Pop();
        if (BishOperator.Call("op_Bool", [result]).ExpectToBe<BishBool>("condition").Value == Reverse) return;
        var pos = frame.FindTag(GoalTag);
        if (pos == -1) throw new ArgumentException($"No such tag: {GoalTag}");
        frame.Ip = pos;
    }
}

public record BishBytecodeJumpIfNot(string GoalTag) : BishBytecodeJumpIf(GoalTag, Reverse: true);

public record BishBytecodeCopy : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Peek());
}