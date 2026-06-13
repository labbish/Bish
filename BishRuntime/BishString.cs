using System.Text.RegularExpressions;
using BishUtils;

namespace BishRuntime;

public partial class BishString(string value) : BishObject
{
    public readonly string Value = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string");

    public BishString(char c) : this(new string(c, 1))
    {
    }

    [Builtin("hook")]
    public static BishString New([DefaultNull] BishString? other) => new(other?.Value ?? "");

    [Builtin("op")]
    public static BishString Add(BishString a, BishString b) => new(a.Value + b.Value);

    [Builtin("op")]
    public static BishString Mul(BishObject a, BishObject b)
    {
        if (a is BishString x) return MulHelper(x, b);
        if (b is BishString y) return MulHelper(y, a);
        throw BishException.OfType_Argument(a, StaticType);
    }

    private static BishString MulHelper(BishString s, BishObject b) =>
        b is BishInt x
            ? new BishString(string.Concat(Enumerable.Repeat(s.Value, x.Value)))
            : throw BishException.OfType_Argument(b, BishInt.StaticType);

    [Builtin("op")]
    public static BishBool Eq(BishString a, BishString b) => BishBool.Of(a.Value == b.Value);

    [Builtin]
    public static BishBool Bool(BishString a) => BishBool.Of(a.Value != "");

    [Builtin("op")]
    public static BishString GetIndex(BishString self, BishObject x) => x switch
    {
        BishInt index => new BishString(self.Value[index.Value.Regularize(self.Value.Length)]),
        BishRange range => new BishString(string.Join("",
            range.Regularize(self.Value.Length).ToInts().Select(i => GetIndex(self, i).Value))),
        _ => throw BishException.OfType_Argument(self, BishInt.StaticType)
    };

    [Builtin]
    public static BishStringIterator Iter(BishString self) => new(self.Value);

    [Builtin("hook")]
    public static BishInt Get_length(BishString self) => BishInt.Of(self.Value.Length);

    public static string CallRepr(BishObject obj, BishReprContext ctx) => obj switch
    {
        BishString str => ctx.Debug ? "'" + Regex.Escape(str.Value).Replace("'", @"\'") + "'" : str.Value,
        BishType type => type.Name,
        _ => BishOperator.Call("repr", [obj, ctx]).As<BishString>("repr").Value
    };

    [Builtin]
    public new static BishString Repr(BishObject obj, BishReprContext ctx) => new(CallRepr(obj, ctx));

    public static string CallShow(BishObject obj) => CallRepr(obj, new BishReprContext(false));

    [Builtin]
    public static BishString Show(BishObject obj) => new(CallShow(obj));

    public static string CallDebug(BishObject obj) => CallRepr(obj, new BishReprContext(true));

    [Builtin]
    public static BishString Debug(BishObject obj) => new(CallDebug(obj));

    [Builtin]
    public static BishString Format(BishString self, [Rest] BishList args)
    {
        var index = 0;
        return new BishString(Formatter().Replace(self.Value,
            match => args.List.ElementAtOrDefault(index++) is { } value
                ? CallRepr(value, new BishReprContext(match.Groups[1].Value == "?"))
                : match.Value));
    }

    [Builtin]
    public static BishList Split(BishString self, BishString sep) =>
        new(self.Value.Split(sep.Value).Select(s => new BishString(s)).ToList<BishObject>());

    [Builtin]
    public static BishInt ToCode(BishString self) => BishInt.Of(self.Value[0]);

    [Builtin]
    public static BishString FromCode(BishInt code) => new((char)code.Value);

    [Builtin]
    public static BishString Upper(BishString self) => new(self.Value.ToUpperInvariant());

    [Builtin]
    public static BishString Lower(BishString self) => new(self.Value.ToLowerInvariant());

    [Builtin]
    public static BishString Replace(BishString self, BishString from, BishString to) =>
        new(self.Value.Replace(from.Value, to.Value));

    [Builtin("hook")]
    public static BishType Get_ReprContext(BishObject _) => BishReprContext.StaticType;

    [GeneratedRegex(@"\{(\??)\}")]
    private static partial Regex Formatter();
}

public class BishStringIterator(string value) : BishObject
{
    public string Value => value;
    public int Index;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string.iter");

    [Iter]
    public BishString? Next() => Index < Value.Length ? new BishString(Value[Index++]) : null;
}

public class BishReprContext(bool debug, IList<BishObject>? visited = null, string? circular = null) : BishObject
{
    public readonly bool Debug = debug;
    public readonly IList<BishObject> Visited = (visited ?? []).ToConcurrentList();
    public readonly string Circular = circular ?? "<...>";

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("ReprContext");

    [Builtin("hook")]
    public static BishReprContext New([DefaultNull] BishBool? debug, [DefaultNull] BishString? circular) =>
        new(debug?.Value ?? false, [], circular?.Value);

    [Builtin("hook")]
    public static BishBool Get_debug(BishReprContext self) => BishBool.Of(self.Debug);

    [Builtin("hook")]
    public static BishString Get_circular(BishReprContext self) => new(self.Circular);

    public bool Contains(BishObject obj) => Visited.Contains(obj);

    public BishReprContext Add(BishObject obj) => new(Debug, Visited.Append(obj).ToList(), Circular);

    [Builtin]
    public static BishBool Contains(BishReprContext self, BishObject obj) => BishBool.Of(self.Contains(obj));

    [Builtin]
    public static BishReprContext Add(BishReprContext self, BishObject obj) => self.Add(obj);
}