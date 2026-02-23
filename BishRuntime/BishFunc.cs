namespace BishRuntime;

// TODO: maybe support rest args (after we have an builtin list)
// `Type` will only be used by builtin functions
public record BishArg(string Name, BishType? DefType = null, BishObject? Default = null)
{
    public BishType Type => DefType ?? BishObject.StaticType;

    public override string ToString() =>
        (DefType is null ? "" : DefType.Name + " ") + Name + (Default is null ? "" : "=" + Default);
}

public class BishFunc(string name, List<BishArg> inArgs, Func<List<BishObject>, BishObject> func) : BishObject
{
    public string Name => name;
    public List<BishArg> Args => CheckedArgs(inArgs);
    public Func<List<BishObject>, BishObject> Func => func;

    public static List<BishArg> CheckedArgs(List<BishArg> args)
    {
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
        var minArgs = Args.Count(arg => arg.Default is null);
        if (args.Count > Args.Count) throw BishException.OfArgument_Count(minArgs, Args.Count, args.Count);
        return Args.Select((arg, i) =>
        {
            var got = args.ElementAtOrDefault(i);
            if (got is not null)
                return got.Type.CanAssignTo(arg.Type) ? got : throw BishException.OfType_Argument(got, arg.Type);
            return arg.Default ?? throw BishException.OfArgument_Count(minArgs, Args.Count, args.Count);
        }).ToList();
    }

    public BishFunc Bind(BishObject self)
    {
        if (Args.Count == 0) throw BishException.OfArgument_Bind(this, self);
        return self.Type.CanAssignTo(Args[0].Type)
            ? new BishFunc(Name, Args.Skip(1).ToList(), args => Func([self, ..args]))
            : throw BishException.OfType_Argument(self, Args[0].Type);
    }

    public override BishObject TryCall(List<BishObject> args)
    {
        try
        {
            return Func(Match(args));
        }
        catch (BishException e)
        {
            e.Error.StackTrace.Add(new BishStackLayer(this, args));
            throw;
        }
    }

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("func");

    public override string ToString() => "[Function]";
}