namespace BishRuntime;

// `Type` will only be used by builtin functions
public record BishArg(string Name, BishType? DefType = null, BishObject? Default = null, bool Rest = false)
{
    public BishType Type => DefType ?? BishObject.StaticType;

    public override string ToString() =>
        (DefType is null ? "" : DefType.Name + " ") + (Rest ? "..." : "") + Name +
        (Default is null ? "" : "=" + Default);

    public BishObject? Match(BishObject? arg)
    {
        if (arg is null) return Default;
        return arg.Type.CanAssignTo(Type) ? arg : throw BishException.OfType_Argument(arg, Type);
    }
}

public class BishFunc(string name, List<BishArg> inArgs, Func<List<BishObject>, BishObject> func) : BishObject
{
    public string Name => name;
    public List<BishArg> Args => CheckedArgs(inArgs);
    public Func<List<BishObject>, BishObject> Func => func;
    public BishObject? BoundSelf;

    public static List<BishArg> CheckedArgs(List<BishArg> args)
    {
        var rests = args.Where(arg => arg.Rest).ToList();
        if (rests.Count > 0)
        {
            if (rests.Count > 1) throw BishException.OfArgument_DefineRests();
            if (!args[^1].Rest) throw BishException.OfArgument_DefineRestPos();
            return args.Any(arg => arg.Default is not null) ? throw BishException.OfArgument_DefineRestDefault() : args;
        }

        var repeat = args.GroupBy(arg => arg.Name)
            .Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
        if (repeat is not null) throw BishException.OfArgument_DefineRepeat(repeat);

        var metDefault = false;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg.Default is not null) metDefault = true;
            if (metDefault && arg.Default is null)
                throw BishException.OfArgument_DefineDefault(i, arg.Name);
        }

        return args;
    }

    public List<BishObject> Match(List<BishObject> args)
    {
        if (Args.Any(arg => arg.Rest))
        {
            var normal = Args[..^1];
            var rest = Args[^1];
            if (args.Count < normal.Count) throw BishException.OfArgument_Count(args.Count, min: normal.Count);
            return normal.Select((arg, i) => arg.Match(args[i])!)
                .Concat([rest.Match(new BishList(args[normal.Count..]))!]).ToList();
        }

        var minArgs = Args.Count(arg => arg.Default is null);
        if (args.Count > Args.Count) throw BishException.OfArgument_Count(args.Count, minArgs, Args.Count);
        return Args.Select((arg, i) =>
            arg.Match(args.ElementAtOrDefault(i)) ??
            throw BishException.OfArgument_Count(args.Count, minArgs, Args.Count)).ToList();
    }

    public BishFunc Bind(BishObject self)
    {
        if (Args.Count == 0) throw BishException.OfArgument_Bind(this, self);
        var args1 = Args[0].Rest ? Args : Args.Skip(1).ToList();
        return self.Type.CanAssignTo(Args[0].Type) || Args[0].Rest
            ? new BishFunc(Name, args1, args => Func([self, ..args])) { BoundSelf = self }
            : throw BishException.OfType_Argument(self, Args[0].Type);
    }

    public override BishObject TryCall(List<BishObject> args)
    {
        var match = Match(args);
        try
        {
            return Func(match);
        }
        catch (BishException e)
        {
            e.Error.StackTrace.Add(new BishStackLayer(this, args));
            throw;
        }
    }

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("func");

    public override string ToString() =>
        (BoundSelf is null ? "Function" : $"Bound function [self={BoundSelf}]") + $" {Name}({string.Join(", ", Args)})";
}