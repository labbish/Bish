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

    public void Awake()
    {
        var thread = BishTaskRunner.Threads[Index];
        thread.Tasks.Push(Task);
        thread.Semaphore.Release();
    }

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

    [Builtin("hook")]
    public static BishInt Get_globalInterval() => BishInt.Of(GlobalInterval);

    [Builtin("hook")]
    public static BishBool Get_started() => BishBool.Of(Called != 0);

    [Builtin("hook")]
    public static BishList Get_threads() => new(Threads.ToList<BishObject>());

    [Builtin("hook")]
    public static BishList Get_tasks() => new(GlobalTasks.ToList());

    public static int Count => Environment.ProcessorCount * 2;
    public static int GlobalInterval => 114;
    internal static int Called;
    internal static readonly BishRunnerThread[] Threads = Array(i => new BishRunnerThread(i));
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
        foreach (var thread in Threads) thread.Semaphore.Release();
    }

    public static void Block(BishObject task)
    {
        Add(task);
        var spinner = new SpinWait();
        while (!BishBool.CallToBool(task.GetMember("completed")))
            spinner.SpinOnce();
    }
}

public class BishRunnerThread : BishObject
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("RunnerThread");

    [Builtin("hook")]
    public static BishInt Get_index(BishRunnerThread self) => BishInt.Of(self.Index);

    [Builtin("hook")]
    public static BishList Get_tasks(BishRunnerThread self) => new(self.Tasks.ToList());

    [Builtin("hook")]
    public static BishInt Get_counter(BishRunnerThread self) => BishInt.Of((int)self.Counter);

    internal readonly int Index;
    internal readonly Thread Thread;
    internal readonly TaskDeque Tasks = new();
    internal readonly Semaphore Semaphore = new(0, int.MaxValue);
    internal long Counter;

    public BishRunnerThread(int index)
    {
        Index = index;
        Thread = new Thread(_ => Loop()) { IsBackground = true };
    }

    public void Start() => Thread.Start();

    public void Loop()
    {
        while (true) SingleLoop();
        // ReSharper disable once FunctionNeverReturns
    }

    public void SingleLoop()
    {
        if (GetTask() is { } task) Execute(task);
        else Semaphore.WaitOne(TimeSpan.FromSeconds(2));
    }

    private void Execute(BishObject task)
    {
        // This lock is necessary, because a same task might be waked more than once
        // Another solution is to check whether the task is in deque or running when waking, but this one is simpler :)
        lock (task)
        {
            if (BishBool.CallToBool(task.TryGetMember("cancelled")))
                task.SetMember("completed", BishBool.True);
            else if (!BishBool.CallToBool(task.TryGetMember("completed")))
                task.GetMember("poll").Call([new BishTaskContext(Index, task)]);
        }
    }

    public BishObject? GetTask()
    {
        Interlocked.Increment(ref Counter);
        if (Counter % BishTaskRunner.GlobalInterval == 0 && BishTaskRunner.GlobalTasks.TryPop() is { } global)
            return global;
        if (Tasks.TryPop() is { } self) return self;
        for (var i = 0; i < BishTaskRunner.Count - 1; i++)
            if (BishTaskRunner.Threads[(i + Index) % BishTaskRunner.Count].Tasks.TryPopLast() is { } other)
                return other;
        return BishTaskRunner.GlobalTasks.TryPop();
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

    public List<BishObject> ToList()
    {
        lock (_lock) return _tasks.ToList();
    }
}