using System.Reflection;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;

namespace BishRuntime;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class BuiltinAttribute(string? prefix = null) : Attribute
{
    public string? Prefix => prefix;

    private static string ToLower(string name) => name == "" ? name : char.ToLower(name[0]) + name[1..];

    public string GetName(string name) => Prefix is null ? ToLower(name) : $"{Prefix}_{name}";
}

[AttributeUsage(AttributeTargets.Parameter)]
public class DefaultNullAttribute : Attribute;

public static class BishBuiltinBinder
{
    public static void Bind<TObject>() where TObject : BishObject
    {
        var type = typeof(TObject);
        var staticType = StaticType(type);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                               BindingFlags.FlattenHierarchy))
        {
            var attr = method.GetCustomAttribute<BuiltinAttribute>();
            if (attr == null) continue;
            staticType.Members[attr.GetName(method.Name)] = Builtin(method);
        }
    }

    public static BishMethod Builtin(MethodInfo method)
    {
        var inArgs = method.GetParameters()
            .Select(info => new BishArg(info.Name!, StaticType(info.ParameterType), Default(info)))
            .ToList();
        return new BishMethod(inArgs,
            args => (BishObject)method.InvokeRaw(null,
                args.Select(arg => ReferenceEquals(arg, DefaultNull) ? null : arg).Cast<object>().ToArray())!);
    }

    // It's really strange that this works fine on nullable args
    public static BishType StaticType(Type type) => (BishType)type.GetField("StaticType")!.GetValue(null)!;

    private static BishObject? Default(ParameterInfo info) =>
        info.GetCustomAttribute<DefaultNullAttribute>() is null ? null : DefaultNull;

    public static object? InvokeRaw(this MethodInfo method, object? obj, object?[]? parameters)
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

    private static readonly BishObject DefaultNull = new BishNull(); // Used only as a tag
}