using BishRuntime;

namespace BishLib;

public static class BishThreadModule
{
    public static void Initialize() => BishLib.InitializeModule("thread",
        ("Thread", BishThread.StaticType),
        ("Lock", BishLock.StaticType)
    );
}

public class BishThread(Thread thread) : BishObject
{
    public Thread Thread { get; private set; } = thread;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Thread");

    [Builtin("hook")]
    public static BishThread Create(BishObject _) => new(null!);

    [Builtin("hook")]
    public static void Init(BishThread self, BishObject func) => self.Thread = new Thread(() => func.Call([]));

    [Builtin]
    public static void Start(BishThread self) => self.Thread.Start();

    [Builtin]
    public static void Join(BishThread self, [DefaultNull] BishInt? ms)
    {
        if (ms is null) self.Thread.Join();
        else self.Thread.Join(ms.Value);
    }

    [Builtin]
    public static void Sleep(BishInt ms) => Thread.Sleep(ms.Value);

    [Builtin("hook")]
    public static BishInt Get_id() => BishInt.Of(Environment.CurrentManagedThreadId);
}

public class BishLock(BishObject obj) : BishObject
{
    public BishObject Object { get; private set; } = obj;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Thread");

    [Builtin("hook")]
    public static BishLock Create(BishObject _) => new(null!);

    [Builtin("hook")]
    public static void Init(BishLock self, BishObject obj) => self.Object = obj;

    [Builtin("hook")]
    public static void Enter(BishLock self) => Monitor.Enter(self.Object);

    [Builtin("hook")]
    public static void Exit(BishLock self, BishObject error) => Monitor.Exit(self.Object);
}

// TODO: semaphore (& channel?)