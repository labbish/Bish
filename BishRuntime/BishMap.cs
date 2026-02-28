namespace BishRuntime;

public class Entry(BishObject key, BishObject value)
{
    public BishObject Key = key;
    public BishObject Value = value;

    public override string ToString() => $"{BishOperator.ToString(Key).Value}: {BishOperator.ToString(Value).Value}";
}

public class BishMap(List<Entry> entries) : BishObject
{
    public List<Entry> Entries { get; private set; } = entries;
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("map");

    [Builtin("hook")]
    public static BishMap Create(BishObject _) => new([]);

    [Builtin("hook")]
    public static void Init(BishMap self, [DefaultNull] BishObject? iterable)
    {
        switch (iterable)
        {
            case BishMap map:
                self.Entries = map.Entries.ToList();
                break;
            case not null:
                self.Entries = [];
                self.AddEntries(ToEntries(iterable.ToEnumerable()));
                break;
        }
    }

    private static List<Entry> ToEntries(IEnumerable<BishObject> entries) => entries.Select(entry =>
        entry.ExpectToBe<BishList>("map entry") switch
        {
            { List.Count: 2 } list => new Entry(list.List[0], list.List[1]),
            _ => throw BishException.OfType_Expect("map entry", entry, "list with length 2"),
        }).ToList();

    private BishObject Add(Entry entry)
    {
        var found = Entries.FirstOrDefault(e => BishOperator.Eq(e.Key, entry.Key).Value);
        if (found is not null) found.Value = entry.Value;
        else Entries.Add(entry);
        return entry.Value;
    }

    private void AddEntries(List<Entry> entries)
    {
        foreach (var entry in entries) Add(entry);
    }

    [Builtin("op")]
    public static BishMap Add(BishMap a, BishMap b)
    {
        var result = new BishMap(a.Entries.ToList());
        result.AddEntries(b.Entries);
        return result;
    }

    public override string ToString() => "{" + string.Join(", ", Entries) + "}";

    [Builtin("op")]
    public static BishBool Eq(BishMap a, BishMap b) => new(a.Entries.All(x =>
        b.Entries.Any(y => BishOperator.Eq(x.Key, y.Key).Value && BishOperator.Eq(x.Value, y.Value).Value)));

    [Builtin]
    public static BishBool Bool(BishMap self) => new(self.Entries.Count != 0);

    [Builtin("op")]
    public static BishObject GetIndex(BishMap self, BishObject key)
    {
        var found = self.Entries.FirstOrDefault(e => BishOperator.Eq(e.Key, key).Value);
        return found is not null ? found.Value : throw BishException.OfArgument_KeyNotFound(key);
    }

    [Builtin("op")]
    public static BishObject SetIndex(BishMap self, BishObject key, BishObject value) =>
        self.Add(new Entry(key, value));

    [Builtin("op")]
    public static BishObject DelIndex(BishMap self, BishObject key)
    {
        var found = self.Entries.FirstOrDefault(e => BishOperator.Eq(e.Key, key).Value);
        if (found is null) throw BishException.OfArgument_KeyNotFound(key);
        self.Entries.Remove(found);
        return found.Value;
    }

    [Builtin]
    public static BishMapIterator Iter(BishMap self) => new(self.Entries);

    [Builtin("hook")]
    public static BishInt Get_length(BishMap self) => new(self.Entries.Count);

    [Builtin(special: false)]
    public static BishList Keys(BishMap self) => new(self.Entries.Select(entry => entry.Key).ToList());

    [Builtin(special: false)]
    public static BishList Values(BishMap self) => new(self.Entries.Select(entry => entry.Value).ToList());

    static BishMap() => BishBuiltinBinder.Bind<BishMap>();
}

public class BishMapIterator(List<Entry> entries) : BishObject
{
    public List<Entry> Entries => entries;
    public int Index;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("map.iter");

    [Iter]
    public BishList? Next()
    {
        var entry = Entries.ElementAtOrDefault(Index++);
        return entry is null ? null : new BishList([entry.Key, entry.Value]);
    }

    static BishMapIterator() => BishBuiltinIteratorBinder.Bind<BishMapIterator>();
}