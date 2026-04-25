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

    // TODO: combine: Task<T>[] -> AsyncIterator<T>
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
    private BishObject? _value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.run");

    [Async]
    public BishObject? Poll(BishTaskContext ctx)
    {
        if (_value is not null) return _value;
        new Thread(() =>
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
        }) { IsBackground = true }.Start();
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
    private bool _done;
    // ReSharper disable once NotAccessedField.Local
    private Timer? _timer;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Task.sleep");

    [Async]
    public BishObject? Poll(BishTaskContext ctx)
    {
        if (_done) return BishNull.Instance;
        _timer = new Timer(_ =>
        {
            ctx.Waker.Awake();
            _done = true;
        }, null, ms, Timeout.Infinite);
        return null;
    }
}