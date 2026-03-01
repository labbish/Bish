using System.Text.RegularExpressions;

namespace BishRuntime;

public partial class BishString(string value) : BishObject
{
    public string Value { get; private set; } = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string");

    public BishString(char c) : this(new string(c, 1))
    {
    }


    [Builtin("hook")]
    public static BishString Create(BishObject _) => new("");

    [Builtin("hook")]
    public static void Init(BishString self, [DefaultNull] BishString? other) => self.Value = other?.Value ?? "";

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

    public override string ToString() => Value;

    [Builtin("op")]
    public static BishBool Eq(BishString a, BishString b) => BishBool.Of(a.Value == b.Value);

    [Builtin]
    public static BishBool Bool(BishString a) => BishBool.Of(a.Value != "");

    [Builtin("op")]
    public static BishString GetIndex(BishString self, BishObject x) => x switch
    {
        BishInt index => new BishString(self.Value[index.Value.Regularize(self.Value.Length)]),
        BishRange range => new BishString(string.Join("",
            range.Regularize(self.Value.Length).ToInts().Select(i => GetIndex(self, i)))),
        _ => throw BishException.OfType_Argument(self, BishInt.StaticType)
    };

    [Builtin]
    public static BishStringIterator Iter(BishString self) => new(self.Value);

    [Builtin("hook")]
    public static BishInt Get_length(BishString self) => BishInt.Of(self.Value.Length);

    public static string CallToString(BishObject obj) =>
        BishOperator.Call("toString", [obj]).ExpectToBe<BishString>("toString").Value;

    [Builtin(special: false)]
    public static BishString Format(BishString self, [Rest] BishList args)
    {
        var autoIndex = 0;
        return new BishString(MyRegex().Replace(self.Value, match =>
        {
            var indexValue = match.Groups[1].Value;
            var index = string.IsNullOrEmpty(indexValue) ? autoIndex++ : int.Parse(indexValue);
            if (index < 0 || index >= args.List.Count) return match.Value;
            return CallToString(args.List[index]);
        }));
    }

    [Builtin(special: false)]
    public static BishList Split(BishString self, BishString sep) =>
        new(self.Value.Split(sep.Value).Select(s => new BishString(s)).ToList<BishObject>());

    // TODO: some more string methods

    static BishString() => BishBuiltinBinder.Bind<BishString>();

    [GeneratedRegex(@"\{(\d*)\}")]
    private static partial Regex MyRegex();
}

public class BishStringIterator(string value) : BishObject
{
    public string Value => value;
    public int Index;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string.iter");

    [Iter]
    public BishString? Next() => Index < Value.Length ? new BishString(Value[Index++]) : null;

    static BishStringIterator() => BishBuiltinIteratorBinder.Bind<BishStringIterator>();
}