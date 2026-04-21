using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;

namespace BishRuntime;

public static class BishBuiltinBinder
{
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255")]
    internal static void Initialize() => BuiltinFunctionRegistry.Registry();
    
    internal static object? InvokeRaw(this MethodInfo method, object? obj, object?[]? parameters)
    {
        try
        {
            return method.Invoke(obj, parameters);
        }
        catch (TargetInvocationException exception)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException!).Throw();
            throw; // Make compiler happy
        }
    }
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class IterAttribute : Attribute;

public static class BishBuiltinIteratorBinder
{
    public static void Bind<T>(bool noParent = false) where T : BishObject
    {
        var type = typeof(T);
        var next = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                       .FirstOrDefault(info =>
                           info.Name == "Next" && info.GetCustomAttribute<IterAttribute>() is not null) ??
                   throw new ArgumentException($"Cannot find method `Next` on type {type}");
        Bind(BishType.GetStaticType(type), next, noParent);
    }

    public static void Bind(BishType type, MethodInfo next, bool noParent = false) =>
        Bind(type, self => (BishObject?)next.InvokeRaw(self, []), noParent);

    public static void Bind(BishType type, Func<BishObject, BishObject?> next, bool noParent = false)
    {
        if (!noParent) type.Parents.Add(BishIterator.StaticType);
        type.DefMember("hook_create", new BishFunc("hook_create", [], _ => throw BishException.OfType_BindIter(type)));
        type.DefMember("next", new BishFunc("next", [new BishArg("iter", type)],
            args => next(args[0]) ?? throw BishException.OfIteratorStop()));
        type.DefMember("iter", new BishFunc("iter", [new BishArg("self", type)], args => args[0]));
    }
}