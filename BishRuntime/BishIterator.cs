namespace BishRuntime;

public class BishIterator(IEnumerator<BishObject> iter) : BishObject
{
    public IEnumerator<BishObject> Iter { get; private set; } = iter;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Iterator");

    [Builtin("hook")]
    public static BishIterator Create(BishObject _) => new(null!);

    [Builtin("hook")]
    public static void Init(BishIterator self, BishObject iter) => self.Iter = iter.ToEnumerator();

    [Iter]
    public BishObject? Next() => Iter.MoveNext() ? Iter.Current : null;

    [Builtin("hook")]
    public static BishIterator Get_entries(BishObject self) =>
        new(self.ToEnumerator().Map((item, i) => new BishList([BishInt.Of(i), item])));

    [Builtin(special: false)]
    public static BishIterator Map(BishObject self, BishObject func) =>
        new(self.ToEnumerator().Map(item => func.Call([item])));

    [Builtin(special: false)]
    public static BishIterator Filter(BishObject self, BishObject func) =>
        new(self.ToEnumerator().Filter(item => BishBool.CallToBool(func.Call([item]))));

    [Builtin(special: false)]
    public static BishIterator Take(BishObject self, BishInt count) => new(self.ToEnumerator().Take(count.Value));

    [Builtin(special: false)]
    public static BishIterator Skip(BishObject self, BishInt count) => new(self.ToEnumerator().Skip(count.Value));

    [Builtin(special: false)]
    public static BishIterator FlatMap(BishObject self, BishObject func) =>
        new(self.ToEnumerator().FlatMap(item => func.Call([item]).ToEnumerable()));


    [Builtin(special: false)]
    public static BishObject Reduce(BishObject self, BishObject func, [DefaultNull] BishObject? init) =>
        self.ToEnumerator().Reduce((acc, x) => func.Call([acc, x]), init);

    [Builtin(special: false)]
    public static void Foreach(BishObject self, BishObject func) =>
        self.ToEnumerator().Foreach(item => func.Call([item]));

    [Builtin(special: false)]
    public static BishList ToList(BishObject self) => new(self.ToEnumerator().ToList());

    private static Predicate<BishObject> WrapPredicate(BishObject? predicate) => item =>
        BishBool.CallToBool(predicate is null ? item : predicate.Call([item]));

    [Builtin(special: false)]
    public static BishBool All(BishObject self, [DefaultNull] BishObject? predicate) =>
        BishBool.Of(self.ToEnumerator().All(WrapPredicate(predicate)));

    [Builtin(special: false)]
    public static BishBool Any(BishObject self, [DefaultNull] BishObject? predicate) =>
        BishBool.Of(self.ToEnumerator().Any(WrapPredicate(predicate)));

    [Builtin(special: false)]
    public static BishObject Find(BishObject self, BishObject predicate) =>
        self.ToEnumerator().Find(WrapPredicate(predicate));

    [Builtin(special: false)]
    public static BishBool Contains(BishObject self, BishObject obj) =>
        BishBool.Of(self.ToEnumerator().Any(item => BishOperator.Eq(item, obj)));

    [Builtin(special: false)]
    public static BishString Join(BishObject self, [DefaultNull] BishString? sep) =>
        new(string.Join(sep?.Value ?? "", ToList(self).List.Select(BishString.CallToString)));

    [Builtin(special: false)]
    public static BishIterator Concat(BishObject self, [Rest] BishList others) =>
        new(self.ToEnumerator().Concat(others.List.Select(iter => iter.ToEnumerator())));

    static BishIterator()
    {
        BishBuiltinIteratorBinder.Bind<BishIterator>(noParent: true);
        BishBuiltinBinder.Bind<BishIterator>();
    }
}

public static class EnumeratorHelper
{
    extension(IEnumerator<BishObject> iter)
    {
        public IEnumerator<BishObject> Map(Func<BishObject, BishObject> func) => iter.Map((item, _) => func(item));

        public IEnumerator<BishObject> Map(Func<BishObject, int, BishObject> func)
        {
            var i = 0;
            while (iter.MoveNext())
                yield return func(iter.Current, i++);
        }

        public IEnumerator<BishObject> Filter(Func<BishObject, bool> predicate)
        {
            while (iter.MoveNext())
                if (predicate(iter.Current))
                    yield return iter.Current;
        }

        public IEnumerator<BishObject> Take(int count)
        {
            if (count <= 0) yield break;
            var taken = 0;
            while (iter.MoveNext())
            {
                yield return iter.Current;
                if (++taken == count) break;
            }
        }

        public IEnumerator<BishObject> Skip(int count)
        {
            if (count <= 0)
                while (iter.MoveNext())
                    yield return iter.Current;
            var skipped = 0;
            while (iter.MoveNext())
            {
                if (skipped++ < count) continue;
                yield return iter.Current;
            }
        }

        public IEnumerator<BishObject> FlatMap(Func<BishObject, IEnumerable<BishObject>> func)
        {
            while (iter.MoveNext())
                foreach (var item in func(iter.Current))
                    yield return item;
        }

        public BishObject Reduce(Func<BishObject, BishObject, BishObject> func, BishObject? init)
        {
            BishObject result;
            if (init is not null) result = init;
            else
            {
                if (!iter.MoveNext()) throw BishException.OfArgument_IndexOutOfBound(0, 0);
                result = iter.Current;
            }

            while (iter.MoveNext()) result = func(result, iter.Current);
            return result;
        }

        public void Foreach(Action<BishObject> func)
        {
            while (iter.MoveNext()) func(iter.Current);
        }

        public List<BishObject> ToList()
        {
            List<BishObject> result = [];
            iter.Foreach(result.Add);
            return result;
        }

        public bool All(Predicate<BishObject> predicate)
        {
            while (iter.MoveNext())
                if (!predicate(iter.Current))
                    return false;
            return true;
        }

        public bool Any(Predicate<BishObject> predicate)
        {
            while (iter.MoveNext())
                if (predicate(iter.Current))
                    return true;
            return false;
        }

        public BishObject Find(Predicate<BishObject> predicate)
        {
            while (iter.MoveNext())
                if (predicate(iter.Current))
                    return iter.Current;
            return BishNull.Instance;
        }

        public IEnumerator<BishObject> Concat(params IEnumerable<IEnumerator<BishObject>> others)
        {
            while (iter.MoveNext()) yield return iter.Current;
            foreach (var other in others)
                while (other.MoveNext())
                    yield return other.Current;
        }
    }
}