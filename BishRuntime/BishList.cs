namespace BishRuntime;

public class BishList(List<BishObject> list) : BishObject
{
    public List<BishObject> List { get; private set; } = list;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("list");

    [Builtin("hook")]
    public static BishList Create() => new([]);

    [Builtin("hook")]
    public static void Init(BishList self, [DefaultNull] BishList? other) => self.List = other?.List.ToList() ?? [];

    [Builtin("op")]
    public static BishList Add(BishList a, BishList b) => new([..a.List, ..b.List]);

    [Builtin("op")]
    public static BishList Mul(BishObject a, BishObject b)
    {
        if (a is BishList x) return MulHelper(x, b);
        if (b is BishList y) return MulHelper(y, a);
        throw BishException.OfType_Argument(a, StaticType);
    }

    private static BishList MulHelper(BishList s, BishObject b)
    {
        return b is BishInt x
            ? new BishList(Enumerable.Repeat(s.List, x.Value).SelectMany(l => l).ToList())
            : throw BishException.OfType_Argument(b, BishInt.StaticType);
    }

    public override string ToString() => "[" + string.Join(", ", List.Select(item => item.ToString())) + "]";

    [Builtin("op")]
    public static BishBool Eq(BishList a, BishList b) => new(a.List.Count == b.List.Count && a.List.Zip(b.List)
        .All(pair => BishOperator.Call("op_Eq", [pair.First, pair.Second])
            .ExpectToBe<BishBool>($"{pair.First} == {pair.Second}").Value));

    [Builtin("op")]
    public static BishBool Bool(BishList a) => new(a.List.Count != 0);

    private int CheckedIndex(int index) => index < List.Count
        ? index
        : throw BishException.OfArgument_IndexOutOfBound(this, index);

    [Builtin("op")]
    public static BishObject GetIndex(BishList a, BishInt b) => a.List[a.CheckedIndex(b.Value)];

    [Builtin("op")]
    public static BishObject SetIndex(BishList a, BishInt b, BishObject value) =>
        a.List[a.CheckedIndex(b.Value)] = value;

    [Builtin("op")]
    public static void DelIndex(BishList a, BishInt b) => a.List.RemoveAt(a.CheckedIndex(b.Value));

    [Builtin("op")]
    public static BishListIterator Iter(BishList self) => new(self.List);

    [Builtin("hook")]
    public static BishInt Get_length(BishList self) => new(self.List.Count);

    [Builtin(special: false)]
    public static void Add(BishList self, BishObject item) => self.List.Add(item);

    // TODO: some more methods

    static BishList() => BishBuiltinBinder.Bind<BishList>();
}

public class BishListIterator(List<BishObject> list) : BishObject
{
    public List<BishObject> List => list;
    public int Index;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("list.iter");

    [Iter]
    public BishObject? Next() => List.ElementAtOrDefault(Index++);

    static BishListIterator() => BishBuiltinIteratorBinder.Bind<BishListIterator>();
}