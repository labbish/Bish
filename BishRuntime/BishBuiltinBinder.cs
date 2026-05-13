using System.Runtime.CompilerServices;

namespace BishRuntime;

[AttributeUsage(AttributeTargets.Method)]
#pragma warning disable CS9113
public class BuiltinAttribute(string? prefix = null, string? tag = null) : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class PassCallerAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public class DefaultNullAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public class RestAttribute : Attribute;

public class DefaultNull : BishObject;

[AttributeUsage(AttributeTargets.Method)]
public class IterAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class AsyncAttribute : Attribute;

public static class BishBuiltinBinder
{
    public static void Init() => RuntimeHelpers.RunClassConstructor(typeof(BishBuiltinBinder).TypeHandle);

    static BishBuiltinBinder()
    {
        BuiltinsRegistry.Register();
        BytecodeParserRegistry.Register();
    }
}

public static class BishBuiltinIteratorBinder
{
    public static void Bind(BishType type, Func<BishObject, BishObject?> next, bool noParent = false)
    {
        if (!noParent) type.ParentsProxy.Add(BishIterator.Type);
        type.DefMember("next", new BishFunc("next", [new BishArg("self")],
            args => next(args[0]) ?? BishIterator.Stop.Instance));
        type.DefMember("iter", new BishFunc("iter", [new BishArg("self")], args => args[0]));
    }
}

public static class BishBuiltinTaskBinder
{
    public static void Awake(this BishObject ctx) => ctx.GetMember("waker").GetMember("awake").Call([]);

    public static void Bind(BishType type, Func<BishObject, BishObject, BishObject?> poll)
    {
        type.ParentsProxy.Add(BishTask.StaticType);
        type.DefMember("poll", new BishFunc("poll", [new BishArg("self"), new BishArg("ctx")], args =>
        {
            var self = args[0];
            var ctx = args[1];
            try
            {
                if (poll(self, ctx) is { } result)
                {
                    self.SetMember("result", result);
                    self.SetMember("completed", BishBool.True);
                    ctx.Awake();
                }
            }
            catch (BishException e)
            {
                self.SetMember("result", new BishErrorResult(e.Error));
                self.SetMember("completed", BishBool.True);
                ctx.Awake();
            }

            return BishNull.Instance;
        }));
    }
}

public interface IBishAsyncIterator
{
    public BishObject? NextPoll(BishObject ctx);
}

public class BishAsyncIteratorTask(IBishAsyncIterator parent) : BishTask
{
    public override BishType DefaultType => StaticType;
    public new static readonly BishType StaticType = new("async.iter");

    [Async]
    public BishObject? Poll(BishObject ctx) => parent.NextPoll(ctx);
}