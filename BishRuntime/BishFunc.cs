using System.Reflection;
using System.Runtime.ExceptionServices;
using JetBrains.Annotations;

namespace BishRuntime;

// TODO: maybe support rest args (after we have an builtin list)
// `Type` will only be used by builtin functions
public record BishArg(string Name, BishType? DefType = null, BishObject? Default = null)
{
    public BishType Type => DefType ?? BishObject.StaticType;
}

public class BishFunc(List<BishArg> inArgs, Func<List<BishObject>, BishObject> func) : BishObject
{
    public List<BishArg> Args => CheckedArgs(inArgs);
    public Func<List<BishObject>, BishObject> Func => func;

    public static List<BishArg> CheckedArgs(List<BishArg> args)
    {
        var repeat = args.GroupBy(arg => arg.Name)
            .Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
        if (repeat is not null) throw BishArgumentException.OfDefineRepeat(repeat);

        var metDefault = false;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg.Default is not null) metDefault = true;
            if (metDefault && arg.Default is null)
                throw BishArgumentException.OfDefineDefault(i, arg);
        }

        return args;
    }

    public List<BishObject> Match(List<BishObject> args)
    {
        var minArgs = Args.Count(arg => arg.Default is not null);
        if (args.Count > Args.Count) throw BishArgumentException.OfCount(minArgs, Args.Count, args.Count);
        return Args.Select((arg, i) =>
        {
            var got = args.ElementAtOrDefault(i);
            if (got is not null)
                return got.Type.CanAssignTo(arg.Type) ? got : throw BishArgumentException.OfType(got, arg.Type);
            return arg.Default ?? throw BishArgumentException.OfCount(minArgs, Args.Count, args.Count);
        }).ToList();
    }

    public override BishObject TryCall(List<BishObject> args) => Func(Match(args));

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("func");

    public override string ToString()
    {
        return "[Function]";
    }
}

public class BishMethod(List<BishArg> inArgs, Func<List<BishObject>, BishObject> func) : BishFunc(inArgs, func)
{
    public BishFunc Bind(BishObject self)
    {
        if (Args.Count == 0) throw BishArgumentException.OfBind(this, self);
        return self.Type.CanAssignTo(Args[0].Type)
            ? new BishFunc(Args.Skip(1).ToList(), args => Func([self, ..args]))
            : throw new BishTypeException(self, Args[0].Type);
    }
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class BuiltinAttribute(string? prefix = null) : Attribute
{
    public string? Prefix => prefix;

    private static string ToLower(string name) => name == "" ? name : char.ToLower(name[0]) + name[1..];

    public string GetName(string name) => Prefix is null ? ToLower(name) : $"{Prefix}_{name}";
}

public static class BuiltinBinder
{
    public static void Bind<TObject>() where TObject : BishObject
    {
        var type = typeof(TObject);
        var staticType = StaticType(type);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            var attr = method.GetCustomAttribute<BuiltinAttribute>();
            if (attr != null) staticType.Members[attr.GetName(method.Name)] = Builtin(method);
        }
    }

    public static BishMethod Builtin(MethodInfo method)
    {
        // TODO: optional args?
        var inArgs = method.GetParameters()
            .Select(info => new BishArg(info.Name!, StaticType(info.ParameterType)))
            .ToList();
        return new BishMethod(inArgs, args => (BishObject)method.InvokeRaw(null, args.Cast<object>().ToArray())!);
    }

    public static BishType StaticType(Type type)
    {
        return (BishType)type.GetField("StaticType")!.GetValue(null)!;
    }

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
}