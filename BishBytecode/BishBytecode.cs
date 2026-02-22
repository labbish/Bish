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

public record BishBytecodeEmpty : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Clear();
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

public record BishBytecodeCall(int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var func = frame.Stack.Pop();
        var args = frame.Stack.Pop(Argc).Reversed();
        frame.Stack.Push(func.Call(args));
    }
}

public record BishBytecodeOp(string Op, int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop(Argc).Reversed();
        frame.Stack.Push(BishOperator.Call(Op, args));
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
        var pos = frame.Bytecodes.FindIndex(x => x.Tag == GoalTag);
        if (pos == -1) throw new ArgumentException($"No such tag: {GoalTag}");
        frame.Ip = pos;
    }
}

public record BishBytecodeJumpIf(string GoalTag, bool Reverse = false) : BishBytecodeJump(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var result = frame.Stack.Pop();
        if (BishOperator.Call("op_Bool", [result])
                .ExpectToBe<BishBool>("condition").Value == Reverse) return;
        base.Execute(frame);
    }
}

public record BishBytecodeJumpIfNot(string GoalTag) : BishBytecodeJumpIf(GoalTag, Reverse: true);

// Used as a tag
// TODO: support optional args?
public record BishBytecodeFuncStart(string Name, List<string> Args) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var pos = EndPos(frame, frame.Ip);
        if (pos == -1) throw new ArgumentException($"Function definition does not end: {Name}");
        frame.Ip = pos;
    }

    public int EndPos(BishFrame frame, int pos) =>
        frame.Bytecodes.FindIndex(pos, x => x is BishBytecodeFuncEnd end && end.Name == Name);
}

// Used as a tag
public record BishBytecodeFuncEnd(string Name) : BishBytecodeNop;

public record BishBytecodeMakeFunc(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var i = frame.Bytecodes.FindIndex(x => x is BishBytecodeFuncStart start && start.Name == Name);
        var start = (BishBytecodeFuncStart)frame.Bytecodes[i];
        var j = start.EndPos(frame, i);
        var code = frame.Bytecodes[(i + 1)..j];
        var scope = frame.Scope;
        frame.Stack.Push(new BishFunc(start.Args.Select(arg => new BishArg(arg)).ToList(), args =>
        {
            var inner = new BishFrame(code, scope, frame);
            // The first argument is in the top
            foreach (var arg in args.Reversed())
                inner.Stack.Push(arg);
            return inner.Execute();
        }));
    }
}

public record BishBytecodeRet : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.ReturnValue = frame.Stack.Pop();
}

public record BishBytecodeCopy : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Peek());
}

public record BishBytecodeSwap : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var first = frame.Stack.Pop();
        var second = frame.Stack.Pop();
        frame.Stack.Push(first);
        frame.Stack.Push(second);
    }
}