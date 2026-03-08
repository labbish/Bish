using BishRuntime;

namespace BishLib;

public class BishThread(Thread? thread) : BishObject
{
    public Thread Thread { get; private set; } = thread!;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Thread");

    public static readonly BishObject Lock;

    [Builtin("hook")]
    public static BishThread Create(BishObject _) => new(null);

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
    public static void Interrupt(BishThread self) => self.Thread.Interrupt();

    [Builtin(special: false)]
    public static void Sleep(BishInt ms) => Thread.Sleep(ms.Value);

    [Builtin("hook", special: false)]
    public static BishInt Get_id() => BishInt.Of(Environment.CurrentManagedThreadId);

    static BishThread()
    {
        BishBuiltinBinder.Bind<BishThread>();
        Lock = new BishObject
        {
            Members = new Dictionary<string, BishObject>
            {
                ["enter"] = new BishFunc("enter", [new BishArg("object")], args =>
                {
                    Monitor.Enter(args[0]);
                    return BishNull.Instance;
                }),
                ["exit"] = new BishFunc("exit", [new BishArg("object")], args =>
                {
                    Monitor.Exit(args[0]);
                    return BishNull.Instance;
                })
            }
        };
    }
}