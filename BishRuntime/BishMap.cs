using BishUtils;

namespace BishRuntime;

public record Entry(BishObject Key, BishObject Value)
{
    public string Show() => $"{BishString.CallShow(Key)}: {BishString.CallShow(Value)}";
    public string Debug() => $"{BishString.CallDebug(Key)}: {BishString.CallDebug(Value)}";
}

public class BishMap(IList<Entry> entries) : BishObject
{
    public readonly IList<Entry> Entries = entries.ToConcurrentList();
    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("map");

    [Builtin("hook")]
    public static BishMap New([DefaultNull] BishObject? iterable) => new(iterable switch
    {
        null => [],
        BishMap map => map.Entries.ToList(),
        _ => ToEntries(iterable.ToEnumerable())
    });

    public static IList<Entry> ToEntries(IEnumerable<BishObject> entries) => entries.Select(entry =>
        entry.As<BishList>("map entry") switch
        {
            { List.Count: 2 } list => new Entry(list.List[0], list.List[1]),
            _ => throw BishException.OfType_Expect("map entry", entry, "list with length 2"),
        }).ToConcurrentList();

    public virtual BishObject Add(Entry entry)
    {
        var index = Entries.FindIndex(e => BishOperator.Eq(e.Key, entry.Key));
        if (index != -1) Entries[index] = entry;
        else Entries.Add(entry);
        return entry.Value;
    }

    public void AddEntries(IList<Entry> entries)
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

    [Builtin]
    public static BishString Show(BishMap self) =>
        new("{" + string.Join(", ", self.Entries.Select(entry => entry.Show())) + "}");

    [Builtin]
    public static BishString Debug(BishMap self) =>
        new("{" + string.Join(", ", self.Entries.Select(entry => entry.Debug())) + "}");

    [Builtin("op")]
    public static BishBool Eq(BishMap a, BishMap b) => BishBool.Of(a.Entries.All(x =>
        b.Entries.Any(y => BishOperator.Eq(x.Key, y.Key) && BishOperator.Eq(x.Value, y.Value))));

    [Builtin]
    public static BishBool Bool(BishMap self) => BishBool.Of(self.Entries.Count != 0);

    [Builtin("op")]
    public static BishObject GetIndex(BishMap self, BishObject key)
    {
        var found = self.Entries.FirstOrDefault(e => BishOperator.Eq(e.Key, key));
        return found is not null ? found.Value : throw BishException.OfArgument_KeyNotFound(key);
    }

    [Builtin("op")]
    public static BishObject SetIndex(BishMap self, BishObject key, BishObject value) =>
        self.Entries.Any(e => BishOperator.Eq(e.Key, key))
            ? DefIndex(self, key, value)
            : throw BishException.OfArgument_KeyNotFound(key);

    [Builtin("op")]
    public static BishObject DefIndex(BishMap self, BishObject key, BishObject value) =>
        self.Add(new Entry(key, value));

    public virtual BishObject Remove(Entry entry)
    {
        Entries.Remove(entry);
        return entry.Value;
    }

    [Builtin("op")]
    public static BishObject DelIndex(BishMap self, BishObject key)
    {
        var found = self.Entries.FirstOrDefault(e => BishOperator.Eq(e.Key, key));
        return found is null ? throw BishException.OfArgument_KeyNotFound(key) : self.Remove(found);
    }

    [Builtin]
    public static BishMapIterator Iter(BishMap self) => new(self.Entries);

    [Builtin("hook")]
    public static BishInt Get_length(BishMap self) => BishInt.Of(self.Entries.Count);

    [Builtin("hook")]
    public static BishList Get_keys(BishMap self) => new(self.Entries.Select(entry => entry.Key).ToList());

    [Builtin("hook")]
    public static BishList Get_values(BishMap self) => new(self.Entries.Select(entry => entry.Value).ToList());

    [Builtin("hook")]
    public static BishList Get_entries(BishMap self) =>
        new(self.Entries.Select(entry => new BishList([entry.Key, entry.Value])).ToList<BishObject>());
}

public class BishMapIterator(IList<Entry> entries) : BishObject
{
    public readonly IList<Entry> Entries = entries;
    public int Index;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("map.iter");

    [Iter]
    public BishList? Next()
    {
        var entry = Entries.ElementAtOrDefault(Index++);
        return entry is null ? null : new BishList([entry.Key, entry.Value]);
    }
}

public class BishProxyMap(IDictionary<string, BishObject> dictionary)
    : BishMap(dictionary.Select(pair => new Entry(new BishString(pair.Key), pair.Value)).ToList())
{
    public override BishObject Add(Entry entry)
    {
        if (entry.Key is BishString key) dictionary[key.Value] = entry.Value;
        return base.Add(entry);
    }

    public override BishObject Remove(Entry entry)
    {
        if (entry.Key is BishString key) dictionary.Remove(key.Value);
        return base.Remove(entry);
    }
}