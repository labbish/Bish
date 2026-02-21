using System.Reflection;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;

namespace BishRuntime;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class BuiltinAttribute(string? prefix = null, bool special = true) : Attribute
{
    public string? Prefix => prefix;
    public bool Special => special;

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
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            var attr = method.GetCustomAttribute<BuiltinAttribute>();
            if (attr == null) continue;
            var name = attr.GetName(method.Name);
            var func = Builtin(method);
            if (attr.Special) BishOperator.CheckSpecialMethod(name, func);
            staticType.Members[name] = func;
        }
    }

    public static BishFunc Builtin(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var inArgs = parameters
            .Select(info => new BishArg(info.Name!, StaticType(info.ParameterType), Default(info)))
            .ToList();
        return new BishFunc(inArgs,
            args => (BishObject)method.InvokeRaw(null,
                args.Select((arg, i) =>
                        ReferenceEquals(arg, DefaultNull) ? null : ConvertImplicit(arg, parameters[i].ParameterType))
                    .ToArray())!);
    }

    public static object ConvertImplicit(object obj, Type target)
    {
        if (target.IsInstanceOfType(obj)) return obj;
        var source = obj.GetType();
        var method = source.GetImplicitConversionMethod(source, target) ??
                     target.GetImplicitConversionMethod(source, target);
        // We don't throw a BishException because it is user's responsibility to be aware of their bindings' correctness
        return method?.Invoke(null, [obj]) ?? throw new InvalidOperationException();
    }

    private static MethodInfo? GetImplicitConversionMethod(this Type type, Type source, Type target)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(mi =>
            {
                var parameters = mi.GetParameters();
                return mi.Name == "op_Implicit"
                       && mi.ReturnType == target
                       && parameters.Length == 1
                       && parameters[0].ParameterType.IsAssignableFrom(source);
            });
    }

    // It's really strange that this works fine on nullable args
    public static BishType StaticType(Type type) => (BishType)type.GetField("StaticType")!.GetValue(null)!;

    private static BishObject? Default(ParameterInfo info) =>
        info.GetCustomAttribute<DefaultNullAttribute>() is null ? null : DefaultNull;

    private static object? InvokeRaw(this MethodInfo method, object? obj, object?[]? parameters)
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