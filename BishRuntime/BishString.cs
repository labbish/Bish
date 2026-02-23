namespace BishRuntime;

public class BishString(string value) : BishObject
{
    public string Value { get; private set; } = value;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("string");

    public BishString(char c) : this(new string(c, 1))
    {
    }


    [Builtin("hook")]
    public static BishString Create() => new("");

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
    public static BishBool Eq(BishString a, BishString b) => new(a.Value == b.Value);

    [Builtin("op")]
    public static BishBool Bool(BishString a) => new(a.Value != "");

    private int CheckedIndex(int index) => index >= -Value.Length && index < Value.Length
        ? index < 0 ? Value.Length + index : index
        : throw BishException.OfArgument_IndexOutOfBound(this, index);

    [Builtin("op")]
    public static BishString GetIndex(BishString a, BishInt b) => new(a.Value[a.CheckedIndex(b.Value)]);

    [Builtin("op")]
    public static BishStringIterator Iter(BishString self) => new(self.Value);

    [Builtin("hook")]
    public static BishInt Get_length(BishString self) => new(self.Value.Length);

    // TODO: some more string methods

    static BishString() => BishBuiltinBinder.Bind<BishString>();
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