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

public record JumpIf(string GoalTag, bool Reverse = false) : Jump(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var result = frame.Stack.Pop();
        if (BishOperator.Call("op_Bool", [result])
                .ExpectToBe<BishBool>("condition").Value == Reverse) return;
        base.Execute(frame);
    }
}

public record JumpIfNot(string GoalTag) : JumpIf(GoalTag, Reverse: true);

// Used as a tag
// TODO: support optional args?
public record FuncStart(string Name, List<string> Args) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var pos = EndPos(frame, frame.Ip);
        if (pos == -1) throw new ArgumentException($"Function definition does not end: {Name}");
        frame.Ip = pos;
    }

    public int EndPos(BishFrame frame, int pos) =>
        frame.Bytecodes.FindIndex(pos, x => x is FuncEnd end && end.Name == Name);
}

// Used as a tag
public record FuncEnd(string Name) : Nop;

public record MakeFunc(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var i = frame.Bytecodes.FindIndex(x => x is FuncStart start && start.Name == Name);
        var start = (FuncStart)frame.Bytecodes[i];
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