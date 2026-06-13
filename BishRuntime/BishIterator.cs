namespace BishRuntime;

public static class BishIterator
{
    public static readonly BishType Type = new("Iterator");
    public static readonly BishType AsyncType = new("AsyncIterator");

    public class Stop : BishObject
    {
        public static readonly Stop Instance = new();

        private Stop()
        {
        }

        [Builtin]
        public static BishString Repr(Stop _, BishReprContext __) => new("IteratorStop");
    }
}

internal static class IndexHelper
{
    extension(int index)
    {
        internal int Regularize(int length, bool check = true)
        {
            if ((index >= -length && index < length) || !check) return index + (index < 0 ? length : 0);
            throw BishException.OfArgument_IndexOutOfBound(length, index);
        }
    }
}

public static class IteratorHelper
{
    public static IEnumerator<BishObject> GetEnumerator(BishObject iterator)
    {
        while (true)
        {
            var result = iterator.GetMember("next").Call([]);
            if (result is BishIterator.Stop) yield break;
            yield return result;
        }
    }

    extension(BishObject obj)
    {
        public BishObject Iterator() => BishOperator.Call("iter", [obj]);
        public IEnumerator<BishObject> ToEnumerator() => GetEnumerator(obj.Iterator());

        public IEnumerable<BishObject> ToEnumerable()
        {
            var enumerator = obj.ToEnumerator();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
}