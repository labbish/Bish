using System.Reflection;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;

namespace BishRuntime;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class BuiltinAttribute(string? prefix = null, bool special = true, string? tag = null) : Attribute
{
    public string? Prefix => prefix;
    public bool Special => special;
    public string? Tag => tag;

    private static string ToLower(string name) => name == "" ? name : char.ToLower(name[0]) + name[1..];

    public string GetName(string name) => (Prefix is null ? "" : Prefix + "_") + ToLower(name);
}

[AttributeUsage(AttributeTargets.Parameter)]
public class DefaultNullAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public class RestAttribute : Attribute;

public static class BishBuiltinBinder
{
    public static void Bind<TObject>() where TObject : BishObject
    {
        var type = typeof(TObject);
        var staticType = BishType.GetStaticType(type);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            var attr = method.GetCustomAttribute<BuiltinAttribute>();
            if (attr == null) continue;
            var name = attr.GetName(method.Name);
            var func = Builtin(name, method, attr.Tag);
            if (attr.Special) BishOperator.CheckSpecialMethod(name, func);
            staticType.Members[name] = func;
        }
    }

    public static BishFunc Builtin(string name, Delegate method, string? tag = null) =>
        Builtin(name, method.Method, tag);

    public static BishFunc Builtin(string name, MethodInfo method, string? tag = null)
    {
        var parameters = method.GetParameters();
        var inArgs = parameters
            .Select(info => new BishArg(info.Name!, BishType.GetStaticType(info.ParameterType), Default(info),
                Rest: info.GetCustomAttribute<RestAttribute>() is not null))
            .ToList();

        BishObject Func(List<BishObject> args)
        {
            try
            {
                var converted = args.Select((arg, i) =>
                        arg is DefaultNull ? null : ConvertImplicit(arg, parameters[i].ParameterType))
                    .ToArray();
                return (BishObject?)method.InvokeRaw(null, converted) ?? BishNull.Instance;
            }
            catch (Exception e)
            {
                if (e is BishException) throw;
                throw new Exception($"Error occured while invoking method {name} ({method})", e);
            }
        }

        return new BishFunc(name, inArgs, Func, tag);
    }

    public static object ConvertImplicit(BishObject obj, Type target)
    {
        if (target.IsInstanceOfType(obj)) return obj;
        var source = obj.GetType();
        var method = source.GetImplicitConversionMethod(source, target) ??
                     target.GetImplicitConversionMethod(source, target);
        // This might happen when Type doesn't match with underlying type
        return method?.Invoke(null, [obj]) ??
               throw BishException.OfType_Argument(obj, BishType.GetStaticType(target));
    }

    private static MethodInfo? GetImplicitConversionMethod(this Type type, Type source, Type target)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(info =>
            {
                var parameters = info.GetParameters();
                return info.Name == "op_Implicit"
                       && info.ReturnType == target
                       && parameters.Length == 1
                       && parameters[0].ParameterType.IsAssignableFrom(source);
            });
    }

    private static DefaultNull? Default(ParameterInfo info) =>
        info.GetCustomAttribute<DefaultNullAttribute>() is null ? null : new DefaultNull();

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

internal class DefaultNull : BishObject;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class IterAttribute : Attribute;

public static class BishBuiltinIteratorBinder
{
    public static void Bind<T>() where T : BishObject
    {
        var type = typeof(T);
        var next = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                       .FirstOrDefault(info =>
                           info.Name == "Next" && info.GetCustomAttribute<IterAttribute>() is not null) ??
                   throw new ArgumentException($"Cannot find method `Next` on type {type}");
        Bind(BishType.GetStaticType(type), next);
    }

    public static void Bind(BishType type, MethodInfo next) =>
        Bind(type, self => (BishObject?)next.InvokeRaw(self, []));

    public static void Bind(BishType type, Func<BishObject, BishObject?> next)
    {
        type.SetMember("hook_create", new BishFunc("hook_create", [], _ =>
            throw BishException.OfType($"Cannot manually create {type.Name}; did you mean to call .iter()?", [])));
        type.SetMember("next",
            new BishFunc("next", [new BishArg("iter", type)],
                args => next(args[0]) ?? throw BishException.OfIteratorStop()));
        type.SetMember("iter", new BishFunc("iter",
            [new BishArg("self", type)], args => args[0]));
    }
}