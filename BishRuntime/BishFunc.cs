using System.Diagnostics.CodeAnalysis;
using BishUtils;

namespace BishRuntime;

public record Arg<T>(string Name, T? Default = null, bool Rest = false) where T : class;

public record BishArg(string Name, BishObject? Default = null, bool Rest = false) : Arg<BishObject>(Name, Default, Rest)
{
    public override string ToString() => (Rest ? ".." : "") + Name + (Default is null ? "" : ":" + Default);

    [return: NotNullIfNotNull(nameof(arg))]
    public BishObject? Match(BishObject? arg) => arg ?? Default;
}

public class BishArgObject(BishArg arg) : BishObject
{
    public BishArg Arg = arg;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Arg");

    [Builtin("hook")]
    public static BishArgObject New(BishString name, [DefaultNull] BishType? type) =>
        new(new BishArg(name.Value, type));

    [Builtin]
    public static BishArgObject Default(BishArgObject self, BishObject value)
    {
        self.Arg = self.Arg with { Default = value };
        return self;
    }

    [Builtin]
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
    public static BishBool Get_hasDefault(BishArgObject self) => BishBool.Of(self.Arg.Default is not null);

    [Builtin("hook")]
    public static BishObject? Get_defaultValue(BishArgObject self) => self.Arg.Default;

    [Builtin("hook")]
    public static BishBool Get_isRest(BishArgObject self) => BishBool.Of(self.Arg.Rest);
}

public abstract class BishFunc(
    string name,
    IList<BishArg> inArgs,
    string? tag = null,
    bool passCaller = false) : BishObject
{
    public string? Tag => tag;
    public string Name = name;

    public IList<BishArg> Args = CheckedArgs<BishArg, BishObject>(inArgs).ToConcurrentList();

    public bool PassCaller = passCaller;

    public abstract BishObject CallRaw(IList<BishObject> args);

    public static IList<TArg> CheckedArgs<TArg, T>(IList<TArg> args) where TArg : Arg<T> where T : class
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

    private ConcurrentList<BishObject> Match(IList<BishObject> args)
    {
        if (Args.LastOrDefault()?.Rest == true)
        {
            var normal = Args.Slice(0, -1);
            var rest = Args[^1];
            if (args.Count < normal.Count) throw BishException.OfArgument_Count(args.Count, min: normal.Count);
            return normal.Select((arg, i) => arg.Match(args[i]))
                .Concat([rest.Match(new BishList(args.Slice(normal.Count)))]).ToConcurrentList();
        }

        var minArgs = Args.Count(arg => arg.Default is null);
        if (args.Count > Args.Count) throw BishException.OfArgument_Count(args.Count, minArgs, Args.Count);
        return Args.Select((arg, i) =>
            arg.Match(args.ElementAtOrDefault(i)) ??
            throw BishException.OfArgument_Count(args.Count, minArgs, Args.Count)).ToConcurrentList();
    }

    public override BishNativeFunc Bind(BishObject self)
    {
        return Args.Count == 0
            ? throw BishException.OfArgument_Bind(this, self)
            : new BishNativeFunc(Name, Args.Slice(Args[0].Rest ? 0 : 1), args => CallRaw(Trans(args)), Tag);

        IList<BishObject> Trans(IList<BishObject> args) => Args[0].Rest
            ? [new BishList([self, ..args[0].As<BishList>("rest arg").List]), ..args.Slice(1)]
            : [self, ..args];
    }

    [Builtin("hook", tag: "ignore")]
    public static BishFunc Bind(BishFunc func, BishObject obj) => func.Bind(obj);

    public override BishObject TryCall(IList<BishObject> args)
    {
        try
        {
            var match = Match(args);
            return CallRaw(match);
        }
        catch (BishException e)
        {
            e.Error.StackTrace.Add(GetStackLayer(args));
            throw;
        }
    }

    public virtual BishStackLayer GetStackLayer(IList<BishObject> args) => new(this, args);

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Func");

    public override string ToString() => $"Function {Name}({string.Join(", ", Args)})";

    [Builtin("hook")]
    public static BishNativeFunc New(BishString name, BishList args, BishFunc func) =>
        new(name.Value, args.List.Select(arg => arg.As<BishArgObject>("arg").Arg).ToList(),
            list => func.Call([new BishList(list)]));

    [Builtin("op", tag: "ignore")]
    public static BishObject Call(BishFunc func, [Rest] BishList args) => func.Call(args.List.ToList());

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
        self.Args = args.List.Select(arg => arg.As<BishArgObject>("arg").Arg).ToList();
        return args;
    }

    [Builtin]
    public static BishFunc Binds(BishFunc self, [Rest] BishList args) =>
        args.List.Aggregate(self, (current, arg) => current.Bind(arg));

    [Builtin("hook")]
    public static BishType Get_Arg(BishObject _) => BishArgObject.StaticType;

    [Builtin("hook")]
    public static BishBool Get_passCaller(BishFunc self) => BishBool.Of(self.PassCaller);

    [Builtin("hook")]
    public static void Set_passCaller(BishFunc self, BishBool value) => self.PassCaller = value.Value;
}

public class BishNativeFunc(
    string name,
    IList<BishArg> inArgs,
    Func<IList<BishObject>, BishObject> func,
    string? tag = null,
    bool passCaller = false) : BishFunc(name, inArgs, tag, passCaller)
{
    public Func<IList<BishObject>, BishObject> Func => func;

    public override BishObject CallRaw(IList<BishObject> args) => Func(args);
}

public class BishArgsProxyList(IList<BishArg> list) : ProxyList<BishArg>(list)
{
    protected override BishArgObject ToItem(BishArg source) => new(source);

    protected override BishArg ToSource(BishObject item) => item.As<BishArgObject>("arg").Arg;
}