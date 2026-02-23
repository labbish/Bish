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

public static class Jumper
{
    extension(BishFrame frame)
    {
        public int TagPos(string tag)
        {
            var pos = frame.Bytecodes.FindIndex(x => x.Tag == tag);
            return pos == -1 ? throw new ArgumentException($"No such tag: {tag}") : pos;
        }

        public void JumpToTag(string tag)
        {
            frame.Ip = frame.TagPos(tag);
        }
    }
}

public record Jump(string GoalTag) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        frame.JumpToTag(GoalTag);
    }
}

public record JumpIf(string GoalTag) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var result = frame.Stack.Pop();
        if (BishOperator.Call("op_Bool", [result]).ExpectToBe<BishBool>("condition").Value)
            frame.JumpToTag(GoalTag);
    }
}

public record JumpIfNot(string GoalTag) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var result = frame.Stack.Pop();
        if (!BishOperator.Call("op_Bool", [result]).ExpectToBe<BishBool>("condition").Value)
            frame.JumpToTag(GoalTag);
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

public static class TagSlicer
{
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    public record CodeSlice<TStart, TEnd>(int StartPos, TStart Start, int EndPos, TEnd End, List<BishBytecode> Code)
        where TStart : StartTag<TEnd> where TEnd : EndTag
    {
        public BishFrame Execute(BishFrame frame, BishObject? stackTop = null)
        {
            var inner = new BishFrame(Code, new BishScope(frame.Scope), frame);
            if (stackTop is not null) inner.Stack.Push(stackTop);
            inner.Execute();
            return inner;
        }
    }

    extension(BishFrame frame)
    {
        public CodeSlice<TStart, TEnd>? TrySlice<TStart, TEnd>(string name)
            where TStart : StartTag<TEnd> where TEnd : EndTag
        {
            var startPos = frame.Bytecodes.FindIndex(x => x is TStart start && start.Name == name);
            if (startPos == -1) return null;
            var start = (TStart)frame.Bytecodes[startPos];
            var endPos = start.EndPos(frame, startPos);
            var end = (TEnd)frame.Bytecodes[endPos];
            var code = frame.Bytecodes[(startPos + 1)..endPos];
            return new CodeSlice<TStart, TEnd>(startPos, start, endPos, end, code);
        }

        public CodeSlice<TStart, TEnd> Slice<TStart, TEnd>(string name)
            where TStart : StartTag<TEnd> where TEnd : EndTag =>
            frame.TrySlice<TStart, TEnd>(name) ??
            throw new ArgumentException($"Start tag named {name} not found");
    }
}

public abstract record TagBased<TStart, TEnd>(string Name)
    : BishBytecode where TStart : StartTag<TEnd> where TEnd : EndTag
{
    public TagSlicer.CodeSlice<TStart, TEnd> Slice(BishFrame frame) => frame.Slice<TStart, TEnd>(Name);
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
        frame.Stack.Push(new BishFunc(Name,
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
        var inner = slice.Execute(frame);
        var parents = frame.Stack.Pop(ParentCount).Reversed()
            .Select(obj => obj.ExpectToBe<BishType>("parent class")).ToList();
        var type = new BishType(Name, parents);
        foreach (var (key, value) in inner.Scope.Vars) type.SetMember(key, value);
        frame.Stack.Push(type);
    }
}

public record Throw : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        throw new BishException(frame.Stack.Pop().ExpectToBe<BishError>("thrown error"));
    }
}

public record TryStart(string Name) : StartTag<TryEnd>(Name)
{
    public override void Execute(BishFrame frame)
    {
        var trySlice = frame.Slice<TryStart, TryEnd>(Name);
        var catchSlice = frame.TrySlice<CatchStart, CatchEnd>(Name);
        var finallySlice = frame.TrySlice<FinallyStart, FinallyEnd>(Name);

        void HandledFinally(BishFrame? blockFrame = null)
        {
            var result = blockFrame?.ReturnValue;
            if (finallySlice is null)
            {
                frame.ReturnValue = result;
                return;
            }

            var finallyFrame = finallySlice.Execute(frame);
            frame.ReturnValue = finallyFrame.ReturnValue ?? result;
        }

        try
        {
            HandledFinally(trySlice.Execute(frame));
        }
        catch (BishException tryException)
        {
            if (catchSlice is null)
            {
                HandledFinally();
                throw;
            }

            try
            {
                HandledFinally(catchSlice.Execute(frame, tryException.Error));
            }
            catch (BishException)
            {
                HandledFinally();
                throw;
            }
        }

        base.Execute(frame); // Jumps to TryEnd
    }
}

public record TryEnd(string Name) : EndTag(Name);

public record CatchStart(string Name) : StartTag<CatchEnd>(Name);

public record CatchEnd(string Name) : EndTag(Name);

public record FinallyStart(string Name) : StartTag<FinallyEnd>(Name);

public record FinallyEnd(string Name) : EndTag(Name);

public record ForIter(string GoalTag) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var iter = frame.Stack.Peek();
        try
        {
            frame.Stack.Push(iter.GetMember("next").Call([]));
        }
        catch (BishException e)
        {
            if (e.Error.Type.CanAssignTo(BishError.IteratorStopType)) frame.JumpToTag(GoalTag);
            else throw;
        }
    }
}