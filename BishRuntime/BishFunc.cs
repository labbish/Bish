using System.Diagnostics.CodeAnalysis;

namespace BishRuntime;

public record Arg<T>(string Name, T? Default = null, bool Rest = false) where T : class;

// `Type` will only be used by builtin functions
public record BishArg(string Name, Func<BishType?>? TypeProvider = null, BishObject? Default = null, bool Rest = false)
    : Arg<BishObject>(Name, Default, Rest)
{
    public BishArg(string Name, BishType? Type, BishObject? Default = null, bool Rest = false) : this(Name,
        () => Type, Default, Rest)
    {
    }

    public BishType Type => DefType ?? BishObject.StaticType;

    public BishType? DefType
    {
        get => field ??= TypeProvider?.Invoke();
        init;
    }

    public override string ToString() => (Rest ? ".." : "") + Name + (DefType is null ? "" : ": " + DefType.Name) +
                                         (Default is null ? "" : "=" + Default);

    [return: NotNullIfNotNull(nameof(arg))]
    public BishObject? Match(BishObject? arg)
    {
        if (arg is null) return Default;
        return arg.Type.CanAssignTo(Type) ? arg : throw BishException.OfType_Argument(arg, Type);
    }
}

public class BishArgObject(BishArg arg) : BishObject
{
    public BishArg Arg { get; private set; } = arg;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Arg");

    [Builtin("hook")]
    public static BishArgObject Create(BishObject _) => new(null!);

    [Builtin("hook")]
    public static void Init(BishArgObject self, BishString name, [DefaultNull] BishType? type) =>
        self.Arg = new BishArg(name.Value, type);

    [Builtin(special: false)]
    public static BishArgObject Default(BishArgObject self, BishObject value)
    {
        self.Arg = self.Arg with { Default = value };
        return self;
    }

    [Builtin(special: false)]
    public static BishArgObject Rest(BishArgObject self)
    {
        self.Arg = self.Arg with { Rest = true };
        return self;
    }

    [Builtin("hook")]
    public static BishString Get_name(BishArgObject self) => new(self.Arg.Name);

    [Builtin("hook")]
    public static BishString Set_name(BishArgObject self, BishString name)
    {
        self.Arg = self.Arg with { Name = name.Value };
        return name;
    }

    [Builtin("hook")]
    public static BishType Get_type(BishArgObject self) => self.Arg.Type;

    [Builtin("hook")]
    public static BishObject Set_type(BishArgObject self, BishObject type)
    {
        self.Arg = self.Arg with
        {
            DefType = type switch
            {
                BishNull => null,
                BishType t => t,
                _ => throw BishException.OfType_Argument(type, BishType.StaticType)
            }
        };
        return type;
    }

    [Builtin("hook")]
    public static BishBool Get_hasDefault(BishArgObject self) => BishBool.Of(self.Arg.Default is not null);

    [Builtin("hook")]
    public static BishObject Get_defaultValue(BishArgObject self) => self.Arg.Default ?? BishNull.Instance;

    [Builtin("hook")]
    public static BishBool Get_isRest(BishArgObject self) => BishBool.Of(self.Arg.Rest);

    static BishArgObject() => BishBuiltinBinder.Bind<BishArgObject>();
}

public class BishFunc(
    string name,
    List<BishArg> inArgs,
    Func<List<BishObject>, BishObject> func,
    string? tag = null,
    bool noCheck = false) : BishObject
{
    public string? Tag => tag;
    public string Name { get; private set; } = name;
    public List<BishArg> Args { get; private set; } = noCheck ? inArgs : CheckedArgs<BishArg, BishObject>(inArgs);
    public Func<List<BishObject>, BishObject> Func { get; private set; } = func;

    public static List<TArg> CheckedArgs<TArg, T>(List<TArg> args) where TArg : Arg<T> where T : class
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
        if (Args.LastOrDefault()?.Rest == true)
        {
            var normal = Args[..^1];
            var rest = Args[^1];
            if (args.Count < normal.Count) throw BishException.OfArgument_Count(args.Count, min: normal.Count);
            return normal.Select((arg, i) => arg.Match(args[i]))
                .Concat([rest.Match(new BishList(args[normal.Count..]))]).ToList();
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
            ? new BishFunc(Name, args1, args => Func([self, ..args]), Tag)
            : throw BishException.OfType_Argument(self, Args[0].Type);
    }

    public override BishObject TryCall(List<BishObject> args)
    {
        try
        {
            var match = Match(args);
            return Func(match);
        }
        catch (BishException e)
        {
            e.Error.StackTrace.Add(new BishStackLayer(this, args));
            throw;
        }
    }

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Func");

    public override string ToString() => $"Function {Name}({string.Join(", ", Args)})";

    [Builtin("hook")]
    public static BishFunc Create(BishObject _) => new("[uninitialized]", null!, null!, noCheck: true);

    [Builtin("hook")]
    public static void Init(BishFunc self, BishString name, BishList args, BishFunc func)
    {
        self.Name = name.Value;
        self.Args = CheckedArgs<BishArg, BishObject>(args.List
            .Select(arg => arg.ExpectToBe<BishArgObject>("arg").Arg).ToList());
        self.Func = list => func.Call([new BishList(list)]);
    }

    [Builtin("hook")]
    public static BishString Get_name(BishFunc self) => new(self.Name);

    [Builtin("hook")]
    public static BishString Set_name(BishFunc self, BishString name)
    {
        self.Name = name.Value;
        return name;
    }

    [Builtin("hook")]
    public static BishList Get_args(BishFunc self) => new(new BishArgsProxyList(self.Args));

    [Builtin("hook")]
    public static BishList Set_args(BishFunc self, BishList args)
    {
        self.Args = args.List.Select(arg => arg.ExpectToBe<BishArgObject>("arg").Arg).ToList();
        return args;
    }

    [Builtin(special: false)]
    public static BishFunc Bind(BishFunc self, [Rest] BishList args) =>
        args.List.Aggregate(self, (current, arg) => current.Bind(arg));

    static BishFunc() => BishBuiltinBinder.Bind<BishFunc>();
}

public class BishArgsProxyList(List<BishArg> list) : ProxyList<BishArg>(list)
{
    protected override BishArgObject ToItem(BishArg source) => new(source);

    protected override BishArg ToSource(BishObject item) => item.ExpectToBe<BishArgObject>("arg").Arg;
}