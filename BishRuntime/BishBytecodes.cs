using System.Diagnostics.CodeAnalysis;
using BishUtils;

namespace BishRuntime;

[Bytecode]
public record Nop : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
    }
}

[Bytecode]
public record Pop(int Count = 1) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Pop(Count);
}

public abstract record Value : BishBytecode;

[Bytecode]
public record Int(int Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(BishInt.Of(Value));
}

[Bytecode]
public record Num(double Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishNum(Value));
}

[Bytecode]
public record String(string Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishString(Value));
}

[Bytecode]
public record Bool(bool Value) : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(BishBool.Of(Value));
}

[Bytecode]
public record Null : Value
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(BishNull.Instance);
}

[Bytecode]
public record Get(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.GetVar(Name));
}

[Bytecode]
public record GetBuiltin(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(BishBuiltinScope.Instance.GetVar(Name));
}

[Bytecode]
public record Def(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.DefVar(Name, frame.Stack.Pop()));
}

[Bytecode]
public record Move(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope.DefVar(Name, frame.Stack.Pop());
}

[Bytecode]
public record Set(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.SetVar(Name, frame.Stack.Pop()));
}

[Bytecode]
public record Del(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Scope.DelVar(Name));
}

[Bytecode]
public record GetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Pop().GetMember(Name));
}

[Bytecode]
public record SetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var value = frame.Stack.Pop();
        var obj = frame.Stack.Pop();
        frame.Stack.Push(obj.SetMember(Name, value));
    }
}

[Bytecode]
public record DefMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var value = frame.Stack.Pop();
        var obj = frame.Stack.Pop();
        frame.Stack.Push(obj.DefMember(Name, value));
    }
}

[Bytecode]
public record MoveMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var value = frame.Stack.Pop();
        var obj = frame.Stack.Pop();
        obj.DefMember(Name, value);
    }
}

[Bytecode]
public record DelMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Pop().DelMember(Name));
}

public static class PassCallerHelper
{
    extension(BishObject func)
    {
        public bool PassCaller
        {
            get
            {
                try
                {
                    return BishBool.CallToBool(func.TryGetMember("passCaller"));
                }
                catch (BishException)
                {
                    return false;
                }
            }
        }
    }
}

[Bytecode]
public record Call(int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop(Argc);
        var func = frame.Stack.Pop();
        if (func.PassCaller) args = [frame, ..args];
        frame.Stack.Push(func.Call(args));
    }
}

[Bytecode]
public record CallArgs : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop().As<BishList>("args").List.ToList();
        var func = frame.Stack.Pop();
        if (func.PassCaller) args = [frame, ..args];
        frame.Stack.Push(func.Call(args));
    }
}

[Bytecode]
public record Op(string Operator, int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var args = frame.Stack.Pop(Argc);
        frame.Stack.Push(BishOperator.Call(Operator, args));
    }
}

[Bytecode]
public record Inner : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Scope = frame.Scope.CreateInner();
}

[Bytecode]
public record Outer : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        frame.Scope = frame.Scope.Outer ?? throw new ArgumentException("No outer scope");
}

public abstract record Jumper(Tag? GoalTag) : BishBytecode
{
    public void Jump(BishFrame frame)
    {
        if (GoalTag is null) return;
        var pos = frame.Bytecodes.FindIndex(x => x.Tag == GoalTag);
        if (pos == -1) throw new ArgumentException($"No such tag: {GoalTag}");
        frame.Ip = pos;
    }
}

[Bytecode]
public record Jump(Tag GoalTag) : Jumper(GoalTag)
{
    public override void Execute(BishFrame frame) => Jump(frame);
}

[Bytecode]
public record JumpIf(Tag GoalTag, bool Reverse = false) : Jumper(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var result = BishOperator.Call("bool", [frame.Stack.Pop()]);
        if (result.As<BishBool>("condition").Value != Reverse) Jump(frame);
    }
}

[Bytecode]
public record JumpIfNot(Tag GoalTag) : JumpIf(GoalTag, Reverse: true);

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
}

public static class TagSlicer
{
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global")]
    public record CodeSlice<TStart, TEnd>(int StartPos, TStart Start, int EndPos, TEnd End, IList<BishBytecode> Code)
        where TStart : StartTag<TEnd> where TEnd : EndTag
    {
        public BishFrame Execute(BishFrame frame, Action<BishFrame>? before = null)
        {
            var inner = new BishFrame(Code, new BishScope(frame.Scope), frame);
            before?.Invoke(inner);
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
            var code = frame.Bytecodes.Slice(startPos + 1, endPos);
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

[Bytecode]
public record FuncStart(string Name, IList<string> Args) : StartTag<FuncEnd>(Name);

[Bytecode]
public record FuncEnd(string Name) : EndTag(Name);

[Bytecode]
public record MakeFunc(string Name, int DefaultArgc = 0, bool Rest = false, bool IsGen = false, bool IsAsync = false)
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
        BishFrame GetInner() => new(slice.Code, scope, frame);
        frame.Stack.Push(new BishCodedFunc(Name, inArgs, GetInner, IsGen, IsAsync));
    }
}

[Bytecode]
public record Ret : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.ReturnValue = frame.Stack.Pop();
}

[Bytecode]
public record Yield : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        frame.Paused = true;
        if (frame.YieldHandler is null) throw BishException.OfType_Yield();
        frame.YieldHandler(frame.Stack.Pop());
    }
}

[Bytecode]
public record Await : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var value = frame.Stack.Pop();
        if (value.TryGetMember("poll") is null)
        {
            frame.Stack.Push(value);
            return;
        }

        if (BishBool.CallToBool(value.GetMember("completed")))
        {
            var result = value.GetMember("result");
            if (result is BishErrorResult error) throw new BishException(error.Error);
            frame.Stack.Push(result);
            return;
        }

        frame.Ip--;
        frame.Stack.Push(value);
        frame.Paused = true;
        if (frame.AwaitHandler is null) throw BishException.OfType_Await();
        frame.AwaitHandler(value);
    }
}

[Bytecode]
public record Copy : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(frame.Stack.Peek());
}

[Bytecode]
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

[Bytecode]
public record CopyVars : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var goal = frame.Stack.Peek();
        foreach (var (key, value) in frame.Scope.Vars)
            goal.DefMember(key, value);
    }
}

[Bytecode]
public record Throw : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var error = frame.Stack.Pop();
        if (error is BishErrorResult result) throw new BishException(result.Error);
        throw new BishException(error.As<BishError>("thrown error"));
    }
}

[Bytecode]
public record TryStart(string Name) : StartTag<TryEnd>(Name)
{
    public override void Execute(BishFrame frame) => frame.AddErrorHandler(frame.Slice<TryStart, TryEnd>(Name).EndPos,
        error => frame.Stack.Push(new BishErrorResult(error)));
}

[Bytecode]
public record TryEnd(string Name) : EndTag(Name);

[Bytecode]
public record BuildList(int Argc) : BishBytecode
{
    public override void Execute(BishFrame frame) => frame.Stack.Push(new BishList(frame.Stack.Pop(Argc)));
}

[Bytecode]
public record TestType(Tag? GoalTag = null) : Jumper(GoalTag)
{
    public override void Execute(BishFrame frame)
    {
        var type = frame.Stack.Pop().As<BishType>("type");
        var obj = frame.Stack.Pop();
        var result = obj.TryConvert(type);
        frame.Stack.Push(BishBool.Of(result is not null));
        frame.Stack.Push(result ?? BishNull.Instance);
        if (result is null) Jump(frame);
    }
}

[Bytecode]
public record Not : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        frame.Stack.Push(BishBool.Of(!BishBool.CallToBool(frame.Stack.Pop())));
}

[Bytecode]
public record RefEq : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        // ReSharper disable once EqualExpressionComparison
        frame.Stack.Push(BishBool.Of(ReferenceEquals(frame.Stack.Pop(), frame.Stack.Pop())));
}

[Bytecode]
public record ListDeconstruct(int Count, int RestPos = -1, bool Pattern = false) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var obj = frame.Stack.Pop();
        if (obj is not BishList list)
        {
            if (!Pattern) throw BishException.OfType_Expect("deconstruct operant", obj, BishList.StaticType);
            frame.Stack.Push(BishBool.False);
            return;
        }

        var rest = RestPos != -1;
        var count = list.List.Count;
        var min = rest ? Count - 1 : Count;
        int? max = rest ? null : Count;
        if (count < min || count > max)
        {
            if (!Pattern) throw BishException.OfArgument_Count(count, min: Count - 1);
            frame.Stack.Push(BishBool.False);
            return;
        }

        var items = Enumerable.Range(0, Count).Select(i => rest switch
        {
            false => BishInt.Of(i) as BishObject,
            true when i < RestPos => BishInt.Of(i),
            true when i > RestPos => BishInt.Of(i - Count),
            true => new BishRange(i, count + i - Count + 1, 1)
        }).Select(index => BishOperator.Call("op_getIndex", [list, index])).ToConcurrentList();
        if (Pattern)
        {
            foreach (var item in items.Reverse()) frame.Stack.Push(item);
            frame.Stack.Push(BishBool.True);
        }
        else
            for (var i = 0; i < items.Count; i++)
                frame.Scope.DefVar($"${i}", items[i]);
    }
}

[Bytecode]
public record TryDelIndex : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        var index = frame.Stack.Pop();
        var obj = frame.Stack.Pop();
        try
        {
            frame.Stack.Push(BishOperator.Call("op_delIndex", [obj, index]));
            frame.Stack.Push(BishBool.True);
        }
        catch (BishException)
        {
            frame.Stack.Push(BishBool.False);
        }
    }
}

[Bytecode]
public record TryGetMember(string Name) : BishBytecode
{
    public override void Execute(BishFrame frame)
    {
        try
        {
            frame.Stack.Push(frame.Stack.Pop().GetMember(Name));
            frame.Stack.Push(BishBool.True);
        }
        catch (BishException)
        {
            frame.Stack.Push(BishBool.False);
        }
    }
}

[Bytecode]
public record DebugStack : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        Console.WriteLine(string.Join(", ", frame.Stack.ToArray()));
}

[Bytecode]
public record DebugVars : BishBytecode
{
    public override void Execute(BishFrame frame) =>
        Console.WriteLine(string.Join(", ", frame.Scope.GetLookupChain().SelectMany(scope => scope.Vars.Keys)));
}