namespace BishRuntime;

public class BishList(List<BishObject> list) : BishObject
{
    public List<BishObject> List { get; private set; } = list;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("list");

    [Builtin("hook")]
    public static BishList Create(BishObject _) => new([]);

    [Builtin("hook")]
    public static void Init(BishList self, [DefaultNull] BishObject? iterable) => self.List = iterable switch
    {
        BishList list => list.List.ToList(),
        not null => iterable.ToEnumerable().ToList(),
        _ => self.List
    };

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
        .All(pair => BishOperator.Call("op_eq", [pair.First, pair.Second])
            .ExpectToBe<BishBool>($"{pair.First} == {pair.Second}").Value));

    [Builtin("op")]
    public static BishBool Bool(BishList a) => new(a.List.Count != 0);

    [Builtin("op")]
    public static BishObject GetIndex(BishList self, BishObject x) => x switch
    {
        BishInt index => self.List[index.Value.Regularize(self.List.Count)],
        BishRange range => new BishList(range.Regularize(self.List.Count).ToInts().Select(i => GetIndex(self, i))
            .ToList()),
        _ => throw BishException.OfType_Argument(self, BishInt.StaticType)
    };

    [Builtin("op")]
    public static BishObject SetIndex(BishList self, BishObject x, BishObject value)
    {
        switch (x)
        {
            case BishInt index: self.List[index.Value.Regularize(self.List.Count)] = value; break;
            case BishRange range:
                range = range.Regularize(self.List.Count);
                if (value is not BishList list) throw BishException.OfType_Argument(value, StaticType);
                if (range.Step == 1)
                {
                    self.List = [..self.List[..range.Start!.Value], ..list.List, ..self.List[range.End!.Value..]];
                    break;
                }

                var indexes = range.ToInts().ToList();
                if (indexes.Count != list.List.Count)
                    throw BishException.OfArgument(
                        $"Setting {indexes.Count} indexes with {list.List.Count} elements", []);
                foreach (var (i, obj) in indexes.Zip(list.List))
                    SetIndex(self, i, obj);
                break;
            default: throw BishException.OfType_Argument(self, BishInt.StaticType);
        }

        return value;
    }

    [Builtin("op")]
    public static BishObject DelIndex(BishList self, BishObject x)
    {
        var result = GetIndex(self, x);
        switch (x)
        {
            case BishInt index: self.List.RemoveAt(index.Value); break;
            case BishRange range:
                var indexes = range.Regularize(self.List.Count).ToInts().Select(i => i.Value).ToList();
                self.List = self.List.Where((_, i) => !indexes.Contains(i)).ToList();
                break;
            default: throw BishException.OfType_Argument(self, BishInt.StaticType);
        }

        return result;
    }

    [Builtin]
    public static BishListIterator Iter(BishList self) => new(self.List);

    [Builtin("hook")]
    public static BishInt Get_length(BishList self) => new(self.List.Count);

    [Builtin(special: false)]
    public static BishList Add(BishList self, BishObject item)
    {
        self.List.Add(item);
        return self;
    }

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