using System.Collections;

namespace BishRuntime;

public class BishList(IList<BishObject> list) : BishObject
{
    public IList<BishObject> List { get; private set; } = list;
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

    public override string ToString() =>
        "[" + string.Join(", ", List.Select(item => BishOperator.ToString(item).Value)) + "]";

    [Builtin("op")]
    public static BishBool Eq(BishList a, BishList b) => BishBool.Of(a.List.Count == b.List.Count && a.List.Zip(b.List)
        .All(pair => BishOperator.Eq(pair.First, pair.Second)));

    [Builtin]
    public static BishBool Bool(BishList a) => BishBool.Of(a.List.Count != 0);

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
                    var start = range.Start!.Value;
                    var end = range.End!.Value;
                    self.List.RemoveRange(start, end - start);
                    self.List.InsertRange(start, list.List);
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
            case BishInt index: self.List.RemoveAt(index.Value.Regularize(self.List.Count)); break;
            case BishRange range:
                var indexes = range.Regularize(self.List.Count).ToInts()
                    .Select(i => i.Value).OrderDescending().ToList();
                foreach (var index in indexes) self.List.RemoveAt(index);
                break;
            default: throw BishException.OfType_Argument(self, BishInt.StaticType);
        }

        return result;
    }

    [Builtin]
    public static BishListIterator Iter(BishList self) => new(self.List);

    [Builtin("hook")]
    public static BishInt Get_length(BishList self) => BishInt.Of(self.List.Count);

    [Builtin(special: false)]
    public static BishList Add(BishList self, BishObject item)
    {
        self.List.Add(item);
        return self;
    }

    [Builtin(special: false)]
    public static BishList Reverse(BishList self) => new(self.List.ToArray().Reverse().ToList());

    static BishList() => BishBuiltinBinder.Bind<BishList>();
}

public class BishListIterator(IList<BishObject> list) : BishObject
{
    public IList<BishObject> List => list;
    public int Index;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("list.iter");

    [Iter]
    public BishObject? Next() => List.ElementAtOrDefault(Index++);

    static BishListIterator() => BishBuiltinIteratorBinder.Bind<BishListIterator>();
}

// ReSharper disable once InconsistentNaming
public static class IListHelper
{
    extension<T>(IList<T> list)
    {
        public void RemoveRange(int index, int count)
        {
            for (var i = index + count - 1; i >= index; i--) list.RemoveAt(i);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            foreach (var item in items.Reverse()) list.Insert(index, item);
        }
    }
}

public abstract class ProxyList<TSource, TItem>(List<TSource> list) : IList<TItem>
{
    public List<TSource> List => list;

    protected abstract TItem ToItem(TSource source);

    protected abstract TSource ToSource(TItem item);

    protected virtual void OnModify()
    {
    }

    public IEnumerator<TItem> GetEnumerator() => List.Select(ToItem).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(TItem item)
    {
        List.Add(ToSource(item));
        OnModify();
    }

    public void Clear()
    {
        List.Clear();
        OnModify();
    }

    public bool Contains(TItem item) => List.Contains(ToSource(item));

    public void CopyTo(TItem[] array, int arrayIndex)
    {
        for (var i = 0; i < List.Count; i++)
            array[arrayIndex + i] = ToItem(List[i]);
    }

    public bool Remove(TItem item) => List.Remove(ToSource(item));

    public int Count => List.Count;
    public bool IsReadOnly => false;
    public int IndexOf(TItem item) => List.IndexOf(ToSource(item));

    public void Insert(int index, TItem item)
    {
        List.Insert(index, ToSource(item));
        OnModify();
    }

    public void RemoveAt(int index)
    {
        List.RemoveAt(index);
        OnModify();
    }

    public TItem this[int index]
    {
        get => ToItem(List[index]);
        set
        {
            List[index] = ToSource(value);
            OnModify();
        }
    }
}

public abstract class ProxyList<T>(List<T> list) : ProxyList<T, BishObject>(list);