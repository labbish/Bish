using System.Diagnostics.CodeAnalysis;
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
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255")]
    internal static void Initialize()
    {
        BuiltinsRegistry.Register();
        BytecodeParserRegistry.Register();
    }
}

public static class BishBuiltinIteratorBinder
{
    public static void Bind(BishType type, Func<BishObject, BishObject?> next, bool noParent = false)
    {
        if (!noParent) type.ParentsProxy.Add(BishIterator.StaticType);
        type.DefMember("next", new BishFunc("next", [new BishArg("self")],
            args => next(args[0]) ?? throw BishException.OfIteratorStop()));
        type.DefMember("iter", new BishFunc("iter", [new BishArg("self")], args => args[0]));
    }
}

public static class BishBuiltinTaskBinder
{
    public static void Bind(BishType type, Func<BishObject, BishTaskContext, BishObject?> poll)
    {
        type.ParentsProxy.Add(BishTask.StaticType);
        type.DefMember("poll", new BishFunc("poll", [new BishArg("self"), new BishArg("ctx")], args =>
        {
            var self = args[0];
            var ctx = (BishTaskContext)args[1];
            try
            {
                if (poll(self, ctx) is { } result)
                {
                    self.SetMember("completed", BishBool.True);
                    self.SetMember("result", result);
                    ctx.Waker.Awake();
                }
            }
            catch (BishException e)
            {
                self.SetMember("completed", BishBool.True);
                self.SetMember("result", new BishErrorResult(e.Error));
                ctx.Waker.Awake();
            }

            return BishNull.Instance;
        }));
    }
}