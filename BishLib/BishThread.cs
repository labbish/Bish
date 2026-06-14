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
    public readonly Thread Thread = thread;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Thread");

    [Builtin("hook")]
    public static BishThread New(BishObject func) => new(new Thread(() => func.Call(new BishArgs([]))));

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
    public static BishInt Get_id(BishObject _) => BishInt.Of(Environment.CurrentManagedThreadId);

    [Builtin]
    public static BishThread OfRunner(BishRunnerThread thread) => new(thread.Thread);
}

public class BishLock : BishObject
{
    public readonly BishObject Object;

    public BishLock(BishObject obj) => Monitor.Enter(Object = obj);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Thread");

    [Builtin("hook")]
    public static BishLock New(BishObject obj) => new(obj);

    [Builtin]
    public static void Dispose(BishLock self) => Monitor.Exit(self.Object);
}

// TODO: semaphore (& channel?)