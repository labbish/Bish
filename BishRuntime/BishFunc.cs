using System.Diagnostics.CodeAnalysis;
using BishUtils;

namespace BishRuntime;

public record Arg<T>(string Name, T? Default = null, bool Rest = false) where T : class;

public record BishArg(string Name, BishObject? Default = null, bool Rest = false) : Arg<BishObject>(Name, Default, Rest)
{
    public override string ToString() =>
        (Rest ? ".." : "") + Name + (Default is null ? "" : ":" + BishString.CallDebug(Default));

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

public class BishArgs(IList<BishObject> args, BishFrame? caller = null)
{
    public readonly IList<BishObject> Args = args;
    public readonly BishFrame? Caller = caller;
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

    public readonly bool PassCaller = passCaller;

    public abstract BishObject CallRaw(BishArgs args);

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

    private ConcurrentList<BishObject> Match(BishArgs args)
    {
        var list = PassCaller ? [args.Caller ?? BishNull.Instance as BishObject, ..args.Args] : args.Args;
        if (Args.LastOrDefault()?.Rest == true)
        {
            var normal = Args.Slice(0, -1);
            var rest = Args[^1];
            if (list.Count < normal.Count) throw BishException.OfArgument_Count(list.Count, min: normal.Count);
            return normal.Select((arg, i) => arg.Match(list[i]))
                .Concat([rest.Match(new BishList(list.Slice(normal.Count)))]).ToConcurrentList();
        }

        var minArgs = Args.Count(arg => arg.Default is null);
        if (list.Count > Args.Count) throw BishException.OfArgument_Count(list.Count, minArgs, Args.Count);
        return Args.Select((arg, i) =>
            arg.Match(list.ElementAtOrDefault(i)) ??
            throw BishException.OfArgument_Count(list.Count, minArgs, Args.Count)).ToConcurrentList();
    }

    public override BishNativeFunc Bind(BishObject self)
    {
        return Args.Count == 0
            ? throw BishException.OfArgument_Bind(this, self)
            : new BishNativeFunc(Name, Args.Slice(Args[0].Rest ? 0 : 1), args => CallRaw(Trans(args)), Tag, PassCaller);

        BishArgs Trans(BishArgs args) => new(Args[0].Rest
            ? [new BishList([self, ..args.Args[0].As<BishList>("rest arg").List]), ..args.Args.Slice(1)]
            : [self, ..args.Args], args.Caller);
    }

    [Builtin("hook", tag: "ignore")]
    public static BishFunc Bind(BishFunc func, BishObject obj) => func.Bind(obj);

    public override BishObject TryCall(BishArgs args)
    {
        var match = new BishArgs(Match(args), args.Caller);
        return CallRaw(match);
    }

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Func");

    [Builtin]
    public static BishString Repr(BishFunc self, BishReprContext _) =>
        new($"Function {self.Name}({string.Join(", ", self.Args)})");

    private static List<BishArg> ToArgs(BishList args) =>
        args.List.Select(arg => arg.As<BishArgObject>("arg").Arg).ToList();

    [Builtin("hook")]
    public static BishNativeFunc New(BishString name, BishList args, BishFunc func) =>
        new(name.Value, ToArgs(args), a => func.Call(new BishArgs([new BishList(a.Args)], a.Caller)));

    [Builtin]
    public static BishCodedFunc Coded(BishString name, BishList args, BishFrame frame,
        [DefaultNull] BishBool? isGen, [DefaultNull] BishBool? isAsync) =>
        new(name.Value, ToArgs(args), frame, isGen?.Value ?? false, isAsync?.Value ?? false);

    [PassCaller]
    [Builtin("op", tag: "ignore")]
    public static BishObject Call(BishFrame? caller, BishFunc func, [Rest] BishList args) =>
        func.Call(new BishArgs(args.List.ToList(), caller));

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
    public static BishType Get_Arg(BishType _) => BishArgObject.StaticType;
}

public class BishNativeFunc(
    string name,
    IList<BishArg> inArgs,
    Func<BishArgs, BishObject> func,
    string? tag = null,
    bool passCaller = false) : BishFunc(name, inArgs, tag, passCaller)
{
    public Func<BishArgs, BishObject> Func => func;

    public override BishObject CallRaw(BishArgs args) => Func(args);
}

public class BishArgsProxyList(IList<BishArg> list) : ProxyList<BishArg>(list)
{
    protected override BishArgObject ToItem(BishArg source) => new(source);

    protected override BishArg ToSource(BishObject item) => item.As<BishArgObject>("arg").Arg;
}