namespace BishRuntime;

// Task<T> { completed: bool; result: T? | ErrorResult; cancelled?: bool, poll(self, ctx: Context): void; };
// Context { waker: Waker; }; Waker { awake(self): void; };
// `poll` must handle errors on its own; otherwise the process will exit

public class BishTaskContext(int index, BishObject task) : BishObject
{
    public readonly BishWaker Waker = new(index, task);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Context");

    [Builtin("hook")]
    public static BishWaker Get_waker(BishTaskContext self) => self.Waker;
}

public class BishWaker(int index, BishObject task) : BishObject
{
    public int Index => index;
    public BishObject Task => task;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Waker");

    public void Awake() => BishTaskRunner.ThreadTasks[Index].Push(Task);

    [Builtin]
    public static void Awake(BishWaker self) => self.Awake();
}

public static class BishTaskRunner
{
    public static readonly BishType StaticType = new("Runner");

    [Builtin]
    public static BishObject Blocked(BishObject task)
    {
        Block(task);
        var result = task.GetMember("result");
        if (result is BishErrorResult error) throw new BishException(error.Error);
        return result;
    }

    public static int Count => Environment.ProcessorCount * 2;
    public static int GlobalInterval => 114;
    internal static int Called;
    internal static long Counter;
    internal static readonly Thread[] Threads = Array(i => new Thread(() => Loop(i)) { IsBackground = true });
    internal static readonly TaskDeque[] ThreadTasks = Array(_ => new TaskDeque());
    internal static readonly TaskDeque GlobalTasks = new();

    private static T[] Array<T>(Func<int, T> func) => Enumerable.Range(0, Count).Select(func).ToArray();

    public static void Start()
    {
        if (Interlocked.Exchange(ref Called, 1) != 0) return;
        foreach (var thread in Threads) thread.Start();
    }

    public static void Add(BishObject task)
    {
        GlobalTasks.Push(task);
    }

    public static void Block(BishObject task)
    {
        Add(task);
        var spinner = new SpinWait();
        while (!BishBool.CallToBool(task.GetMember("completed")))
            spinner.SpinOnce();
    }

    public static void Loop(int index)
    {
        while (true) SingleLoop(index);
        // ReSharper disable once FunctionNeverReturns
    }

    public static void SingleLoop(int index)
    {
        if (GetTask(index) is { } task) Execute(index, task);
        else Thread.Yield();
    }

    private static void Execute(int index, BishObject task)
    {
        if (BishBool.CallToBool(task.TryGetMember("cancelled")))
            task.SetMember("completed", BishBool.True);
        else if (!BishBool.CallToBool(task.TryGetMember("completed")))
            task.GetMember("poll").Call([new BishTaskContext(index, task)]);
    }

    public static BishObject? GetTask(int index)
    {
        Interlocked.Increment(ref Counter);
        if (Counter % GlobalInterval == 0)
        {
            var globalTask = GlobalTasks.TryPop();
            if (globalTask is not null) return globalTask;
        }

        var selfTask = ThreadTasks[index].TryPop();
        if (selfTask is not null) return selfTask;
        for (var i = 0; i < Count - 1; i++)
        {
            var otherTask = ThreadTasks[(i + index) % Count].TryPopLast();
            if (otherTask is not null) return otherTask;
        }

        return GlobalTasks.TryPop();
    }
}

internal class TaskDeque
{
    private readonly Lock _lock = new();
    private readonly LinkedList<BishObject> _tasks = [];

    public void Push(BishObject task)
    {
        lock (_lock) _tasks.AddLast(task);
        BishTaskRunner.Start();
    }

    public BishObject? TryPop()
    {
        lock (_lock)
        {
            var task = _tasks.First?.Value;
            if (task is not null) _tasks.RemoveFirst();
            return task;
        }
    }

    public BishObject? TryPopLast()
    {
        lock (_lock)
        {
            var task = _tasks.Last?.Value;
            if (task is not null) _tasks.RemoveLast();
            return task;
        }
    }
}