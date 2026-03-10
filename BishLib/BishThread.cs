using BishRuntime;

namespace BishLib;

public static class BishThreadModule
{
    public static BishObject Module => new BishObject
    {
        Members = new Dictionary<string, BishObject>
        {
            ["Thread"] = BishThread.StaticType,
            ["Lock"] = BishLock.StaticType
        }
    };
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

    [Builtin(special: false)]
    public static void Start(BishThread self) => self.Thread.Start();

    [Builtin(special: false)]
    public static void Join(BishThread self, [DefaultNull] BishInt? ms)
    {
        if (ms is null) self.Thread.Join();
        else self.Thread.Join(ms.Value);
    }

    [Builtin(special: false)]
    public static void Sleep(BishInt ms) => Thread.Sleep(ms.Value);

    [Builtin("hook", special: false)]
    public static BishInt Get_id() => BishInt.Of(Environment.CurrentManagedThreadId);

    static BishThread() => BishBuiltinBinder.Bind<BishThread>();
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

    static BishLock() => BishBuiltinBinder.Bind<BishLock>();
}