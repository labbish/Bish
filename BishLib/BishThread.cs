using BishRuntime;

namespace BishLib;

public static class BishThreadModule
{
    public static void Initialize() => BishLib.InitializeModule("thread",
        ("Thread", BishThread.StaticType),
        ("Lock", BishLock.StaticType),
        ("ThreadError", Error)
    );

    public static readonly BishType Error = new("ThreadError", [BishError.StaticType]);
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
    public static void Start(BishThread self) => BishException.Wrapped(BishThreadModule.Error, self.Thread.Start);

    [Builtin]
    public static BishBool Join(BishThread self, [DefaultNull] BishInt? ms) =>
        BishException.Wrapped(BishThreadModule.Error, () =>
        {
            if (ms is not null) return BishBool.Of(self.Thread.Join(ms.Value));
            self.Thread.Join();
            return BishBool.True;
        });

    [Builtin]
    public static void Sleep(BishInt ms) => Thread.Sleep(ms.Value);

    [Builtin("hook")]
    public static BishInt Get_id() => BishInt.Of(Environment.CurrentManagedThreadId);

    [Builtin]
    public static BishThread OfRunner(BishRunnerThread thread) => new(thread.Thread);
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
    public static void Exit(BishLock self, BishObject _) => Monitor.Exit(self.Object);
}

// TODO: semaphore (& channel?)