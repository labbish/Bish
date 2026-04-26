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
}

public class BishCompletedTask(BishObject value) : BishTask
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.completed");

    [Async]
    public BishObject Poll(BishTaskContext _) => value;
}

public class BishErrorTask(BishError error) : BishTask
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.error");

    [Async]
    public BishObject Poll(BishTaskContext _) => new BishErrorResult(error);
}

public class BishRunTask(Func<BishObject> func) : BishTask
{
    private volatile BishObject? _value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.run");

    [Async]
    public BishObject? Poll(BishTaskContext ctx)
    {
        if (_value is not null) return _value;
        Task.Run(() =>
        {
            try
            {
                _value = func();
            }
            catch (BishException e)
            {
                _value = new BishErrorResult(e.Error);
            }

            ctx.Waker.Awake();
        });
        return null;
    }
}

public class BishAllTask(IList<BishObject> tasks) : BishTask
{
    private readonly BishObject?[] _results = new BishObject?[tasks.Count];

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.all");

    [Async]
    public BishObject? Poll(BishTaskContext ctx)
    {
        foreach (var (task, i) in tasks.Enumerate())
        {
            if (_results[i] is not null) continue;
            if (BishBool.CallToBool(task.GetMember("completed")))
                _results[i] = task.GetMember("result");
            task.GetMember("poll").Call([ctx]);
        }

        if (_results.All(result => result is not null)) return new BishList(_results!);
        ctx.Waker.Awake();
        return null;
    }
}

public class BishAnyTask(IList<BishObject> tasks) : BishTask
{
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.any");

    [Async]
    public BishObject? Poll(BishTaskContext ctx)
    {
        foreach (var task in tasks)
        {
            task.GetMember("poll").Call([ctx]);
            if (BishBool.CallToBool(task.GetMember("completed")))
                return task.GetMember("result");
        }

        ctx.Waker.Awake();
        return null;
    }
}

public class BishSleepTask(int ms) : BishTask
{
    private volatile bool _done;

    // ReSharper disable once NotAccessedField.Local
    private Timer? _timer;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.sleep");

    [Async]
    public BishObject? Poll(BishTaskContext ctx)
    {
        if (_done)
        {
            _timer?.Dispose();
            return BishNull.Instance;
        }

        _timer = new Timer(_ =>
        {
            _done = true;
            ctx.Waker.Awake();
        }, null, ms, Timeout.Infinite);
        return null;
    }
}

public class BishMergeTasks(IList<BishObject> tasks) : BishObject, IBishAsyncIterator
{
    private readonly LinkedList<BishObject> _tasks = new(tasks);
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Task.merge");

    [Iter]
    public BishObject Next() => new BishAsyncIteratorTask(this);

    public BishObject? NextPoll(BishTaskContext ctx)
    {
        var task = _tasks.First?.Value;
        if (task is null) return BishIterator.Stop.Instance;
        _tasks.RemoveFirst();
        task.GetMember("poll").Call([ctx]);
        if (BishBool.CallToBool(task.GetMember("completed")))
            return task.GetMember("result");
        _tasks.AddLast(task);
        ctx.Waker.Awake();
        return null;
    }
}

public class BishConcatTasks(IList<BishObject> tasks) : BishObject, IBishAsyncIterator
{
    private readonly BishObject?[] _results = new BishObject?[tasks.Count];
    private int _count;
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("Task.concat");

    [Iter]
    public BishObject Next() => new BishAsyncIteratorTask(this);

    public BishObject? NextPoll(BishTaskContext ctx)
    {
        if (_count == tasks.Count) return BishIterator.Stop.Instance;
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

        ctx.Waker.Awake();
        return null;
    }
}