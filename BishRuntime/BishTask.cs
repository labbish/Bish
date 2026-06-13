using BishUtils;

namespace BishRuntime;

public abstract class BishTask : BishObject
{
    protected BishTask()
    {
        Vars["completed"] = BishBool.False;
        Vars["result"] = BishNull.Instance;
        Vars["cancelled"] = BishBool.False;
    }

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task");

    [Builtin]
    public static void Cancel(BishTask self) => self.SetMember("cancelled", BishBool.True);

    [Builtin]
    public static BishCompletedTask Completed(BishObject value) => new(value);

    [Builtin]
    public static BishErrorTask Error(BishError error) => new(error);

    [Builtin]
    public static BishRunTask Run(BishObject func) => new(() => func.Call([]));

    [Builtin]
    public static BishAllTask All([Rest] BishList tasks) => new(tasks.List);

    [Builtin]
    public static BishAnyTask Any([Rest] BishList tasks) => new(tasks.List);

    [Builtin]
    public static BishSleepTask Sleep(BishInt ms) => new(ms.Value);

    [Builtin]
    public static BishMergeTasks Merge([Rest] BishList tasks) => new(tasks.List);

    [Builtin]
    public static BishConcatTasks Concat([Rest] BishList tasks) => new(tasks.List);

    [Builtin]
    public static BishMapTask Map(BishObject task, BishObject func) => new(task, item => func.Call([item]));
}

public class BishCompletedTask(BishObject value) : BishTask
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.completed");

    [Async]
    public BishObject Poll(BishObject _) => value;
}

public class BishErrorTask(BishError error) : BishTask
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.error");

    [Async]
    public BishObject Poll(BishObject _) => new BishErrorResult(error);
}

public class BishNativeTask(Func<Task<BishObject>> provider) : BishTask
{
    private volatile BishObject? _result;

    private Task? _task;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.native");

    public BishNativeTask(Func<Task> provider) : this(() => provider().ContinueWith<BishObject>(_ => BishNull.Instance))
    {
    }

    [Async]
    public BishObject? Poll(BishObject ctx)
    {
        if (_result is not null) return _result;
        if (_task is not null) return null;
        _task = provider().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var exception = t.Exception.InnerException;
                if (exception is BishException e) _result = new BishErrorResult(e.Error);
                else throw exception!;
            }
            else _result = t.Result;

            ctx.Awake();
        });
        return null;
    }
}

public class BishRunTask(Func<BishObject> func) : BishNativeTask(() => Task.Run(func));

public class BishAllTask(IList<BishObject> tasks) : BishTask
{
    private readonly BishObject?[] _results = new BishObject?[tasks.Count];

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.all");

    [Async]
    public BishObject? Poll(BishObject ctx)
    {
        foreach (var (task, i) in tasks.Enumerate())
        {
            if (_results[i] is not null) continue;
            if (BishBool.CallToBool(task.GetMember("completed")))
                _results[i] = task.GetMember("result");
            task.GetMember("poll").Call([ctx]);
        }

        if (_results.All(result => result is not null)) return new BishList(_results!);
        ctx.Awake();
        return null;
    }
}

public class BishAnyTask(IList<BishObject> tasks) : BishTask
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.any");

    [Async]
    public BishObject? Poll(BishObject ctx)
    {
        foreach (var task in tasks)
        {
            task.GetMember("poll").Call([ctx]);
            if (BishBool.CallToBool(task.GetMember("completed")))
                return task.GetMember("result");
        }

        ctx.Awake();
        return null;
    }
}

public class BishSleepTask(int ms) : BishNativeTask(() => Task.Delay(ms));

public class BishMergeTasks(IList<BishObject> tasks) : BishObject, IBishAsyncIterator
{
    private readonly LinkedList<BishObject> _tasks = new(tasks);
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Task.merge", [BishIterator.AsyncType]);

    [Iter]
    public BishObject Next() => new BishAsyncIteratorTask(this);

    public BishObject? NextPoll(BishObject ctx)
    {
        var task = _tasks.First?.Value;
        if (task is null) return BishIteratorStop.Instance;
        _tasks.RemoveFirst();
        task.GetMember("poll").Call([ctx]);
        if (BishBool.CallToBool(task.GetMember("completed")))
            return task.GetMember("result");
        _tasks.AddLast(task);
        ctx.Awake();
        return null;
    }
}

public class BishConcatTasks(IList<BishObject> tasks) : BishObject, IBishAsyncIterator
{
    private readonly BishObject?[] _results = new BishObject?[tasks.Count];
    private int _count;
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Task.concat", [BishIterator.AsyncType]);

    [Iter]
    public BishObject Next() => new BishAsyncIteratorTask(this);

    public BishObject? NextPoll(BishObject ctx)
    {
        if (_count == tasks.Count) return BishIteratorStop.Instance;
        foreach (var (task, i) in tasks.Enumerate())
        {
            if (i < _count) continue;
            if (_results[i] is not null) continue;
            if (BishBool.CallToBool(task.GetMember("completed")))
                _results[i] = task.GetMember("result");
            task.GetMember("poll").Call([ctx]);
        }

        if (_results[_count] is { } result)
        {
            Interlocked.Increment(ref _count);
            return result;
        }

        ctx.Awake();
        return null;
    }
}

public class BishMapTask(BishObject task, Func<BishObject, BishObject> func) : BishTask
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.map");

    [Async]
    public BishObject? Poll(BishObject ctx)
    {
        if (BishBool.CallToBool(task.GetMember("completed")))
            return func(task.GetMember("result"));
        task.GetMember("poll").Call([ctx]);
        return null;
    }
}