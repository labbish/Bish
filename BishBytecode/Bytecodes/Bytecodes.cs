using System.Diagnostics.CodeAnalysis;
using BishRuntime;

namespace BishBytecode.Bytecodes;

public record Nop : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
    }
}

public record Pop(int Count = 1) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Pop(Count);
}

public record EndStat : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var obj = frame.Stack.Pop();
        frame.EndStatHandler?.Invoke(obj);
    }
}

public abstract record Value : BishBytecode;

public record Int(int Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishInt(Value));
}

public record Num(double Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishNum(Value));
}

public record String(string Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishString(Value));
}

public record Bool(bool Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishBool(Value));
}

public record Null : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(BishNull.Instance);
}

public record Get(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.GetVar(Name));
}

public record Def(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.DefVar(Name, frame.Stack.Pop()));
}

public record Move(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.DefVar(Name, frame.Stack.Pop());
}

public record Set(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.SetVar(Name, frame.Stack.Pop()));
}

public record Del(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.DelVar(Name));
}

public record GetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Pop().GetMember(Name));
}

public record SetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var value = frame.Stack.Pop();
        var obj = frame.Stack.Pop();
        frame.Stack.Push(obj.SetMember(Name, value));
    }
}

public record DelMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Pop().DelMember(Name));
}

public record Call(int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop(Argc);
        var func = frame.Stack.Pop();
        frame.Stack.Push(func.Call(args));
    }
}

public record CallArgs : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop().ExpectToBe<BishList>("args");
        var func = frame.Stack.Pop();
        frame.Stack.Push(func.Call(args.List));
    }
}

public record Op(string Operator, int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop(Argc);
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

public abstract record Jumper(string? GoalTag) : BishBytecode
{
    public string? GoalTag = GoalTag;

    public void Jump(BishFrame frame)
    {
        if (GoalTag is null) return;
        var pos = frame.Bytecodes.FindIndex(x => x.Tag == GoalTag);
        if (pos == -1) throw new ArgumentException($"No such tag: {GoalTag}");
        frame.Ip = pos;
    }
}

public record Jump(string GoalTag) : Jumper(GoalTag)
{
    public override void Execute(BishFrame frame) => Jump(frame);
}

public record JumpIf(string GoalTag, bool Reverse = false) : Jumper(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var result = BishOperator.Call("bool", [frame.Stack.Pop()]);
        if (result.ExpectToBe<BishBool>("condition").Value != Reverse) Jump(frame);
    }
}

public record JumpIfNot(string GoalTag) : JumpIf(GoalTag, Reverse: true);

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

public record EndTag(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
    }
};

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

public record MakeFunc(string Name, int DefaultArgc = 0, bool Rest = false, bool Gen = false)
    : TagBased<FuncStart, FuncEnd>(Name)
{
    public override void Execute(BishFrame frame)
    {
        var slice = Slice(frame);
        var names = slice.Start.Args;
        var scope = frame.Scope;
        var defaults = frame.Stack.Pop(DefaultArgc);
        var inArgs = names
            .Select((arg, i) => new BishArg(arg, Default: defaults.ElementAtOrDefault(^(names.Count - i)),
                Rest: Rest && i == names.Count - 1)).ToList();

        BishObject Func(List<BishObject> args)
        {
            var inner = new BishFrame(slice.Code, scope, frame);
            // The first argument is in the top
            foreach (var arg in args.Reversed()) inner.Stack.Push(arg);
            if (!Gen) return inner.Execute();

            var type = new BishType("gen");
            BishBuiltinIteratorBinder.Bind(type, _ =>
            {
                try
                {
                    inner.Execute();
                }
                catch (BishException e) when (e.Error.Type.CanAssignTo(BishError.YieldValueType))
                {
                    return e.Error.GetMember("value");
                }

                return null;
            });
            return new BishObject(type);
        }

        frame.Stack.Push(new BishFunc(Name, inArgs, Func));
    }
}

public record Ret : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.ReturnValue = frame.Stack.Pop();
}

public record Yield : BishBytecode
{
    public override void Execute(BishFrame frame) => throw BishException.OfYield(frame.Stack.Pop());
}

public record Copy : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Peek());
}

public record Swap(int Count = 1) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var top = frame.Stack.Pop();
        var items = frame.Stack.Pop(Count);
        frame.Stack.Push(top);
        foreach (var item in items)
            frame.Stack.Push(item);
    }
}

public record ClassStart(string Name) : StartTag<ClassEnd>(Name);

public record ClassEnd(string Name) : EndTag(Name);

public static class ClassHelper
{
    public static void MakeClass(this BishFrame frame, string name, TagSlicer.CodeSlice<ClassStart, ClassEnd> slice,
        List<BishObject> parents)
    {
        var inner = slice.Execute(frame);
        var type = new BishType(name, parents.Select(obj => obj.ExpectToBe<BishType>("parent class")).ToList());
        foreach (var (key, value) in inner.Scope.Vars) type.SetMember(key, value);
        frame.Stack.Push(type);
    }
}

public record MakeClass(string Name, int ParentCount = 0) : TagBased<ClassStart, ClassEnd>(Name)
{
    public override void Execute(BishFrame frame) => frame.MakeClass(Name, Slice(frame), frame.Stack.Pop(ParentCount));
}

public record MakeClassArgs(string Name) : TagBased<ClassStart, ClassEnd>(Name)
{
    public override void Execute(BishFrame frame) =>
        frame.MakeClass(Name, Slice(frame), frame.Stack.Pop().ExpectToBe<BishList>("parents").List);
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

public record ForIter(string GoalTag) : Jumper(GoalTag)
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
            if (e.Error.Type.CanAssignTo(BishError.IteratorStopType)) Jump(frame);
            else throw;
        }
    }
}

public record BuildList(int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishList(frame.Stack.Pop(Argc)));
}

public record IsNull : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishBool(frame.Stack.Pop() is BishNull));
}

public record TestType(string? GoalTag = null) : Jumper(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var type = frame.Stack.Pop().ExpectToBe<BishType>("type");
        var obj = frame.Stack.Pop();
        var result = obj.TryConvert(type);
        frame.Stack.Push(new BishBool(result is not null));
        frame.Stack.Push(result ?? BishNull.Instance);
        if (result is null) Jump(frame);
    }
}

public record Not : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        frame.Stack.Push(new BishBool(!BishBool.CallToBool(frame.Stack.Pop())));
}

public record RefEq : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        // ReSharper disable once EqualExpressionComparison
        frame.Stack.Push(new BishBool(ReferenceEquals(frame.Stack.Pop(), frame.Stack.Pop())));
}

public record TryFunc : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var obj = frame.Stack.Pop();
        if (obj is BishNull) frame.Stack.Push(BishNull.Instance);
        var func = obj.ExpectToBe<BishFunc>("operant of try expression");
        frame.Stack.Push(new BishFunc(func.Name, func.Args, args =>
        {
            try
            {
                return func.Call(args);
            }
            catch (BishException)
            {
                return BishNull.Instance;
            }
        }, func.Tag));
    }
}

// ReSharper disable once UnusedType.Global
public record DebugStack : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        Console.WriteLine(string.Join(", ", frame.Stack.ToArray()));
}