namespace BishRuntime;

public class BishString(string value) : BishObject
{
    public string Value { get; private set; } = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string");


    [Builtin("hook")]
    public static BishString Create() => new("");

    [Builtin("hook")]
    public static BishNull Init(BishString self, [DefaultNull] BishString? other)
    {
        self.Value = other?.Value ?? "";
        return BishNull.Instance;
    }

    [Builtin("op")]
    public static BishString Add(BishString a, BishString b)
    {
        return new BishString(a.Value + b.Value);
    }

    [Builtin("op")]
    public static BishString Mul(BishObject a, BishObject b)
    {
        if (a is BishString x) return MulHelper(x, b);
        if (b is BishString y) return MulHelper(y, a);
        throw BishException.OfType_Argument(a, StaticType);
    }

    private static BishString MulHelper(BishString s, BishObject b)
    {
        return b is BishInt x
            ? new BishString(string.Concat(Enumerable.Repeat(s.Value, x.Value)))
            : throw BishException.OfType_Argument(b, BishInt.StaticType);
    }

    public override string ToString() => Value;

    [Builtin("op")]
    public static BishBool Eq(BishString a, BishString b) => new(a.Value == b.Value);

    [Builtin("op")]
    public static BishBool Bool(BishString a)
    {
        return new BishBool(a.Value != "");
    }

    [Builtin("op")]
    public static BishString GetIndex(BishString a, BishInt b)
    {
        return new BishString(new string(a.Value[b.Value], 1));
    }

    // TODO: iterator?

    [Builtin("hook")]
    public static BishInt Get_length(BishString self)
    {
        return new BishInt(self.Value.Length);
    }

    // TODO: some more string methods

    static BishString() => BishBuiltinBinder.Bind<BishString>();
}