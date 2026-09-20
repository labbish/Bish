using System.Numerics;

namespace BishUtils;

public static class EnumerableHelper
{
    extension<T>(IEnumerable<T> enumerable)
    {
        // TODO: replace with .Index()
        public IEnumerable<(T Item, int Index)> Enumerate() => enumerable.Select((x, i) => (x, i));

        public IEnumerable<(T? First, TSecond? Second)> ZipLongest<TSecond>(IEnumerable<TSecond> other)
        {
            using var e1 = enumerable.GetEnumerator();
            using var e2 = other.GetEnumerator();

            while (true)
            {
                var has1 = e1.MoveNext();
                var has2 = e2.MoveNext();

                if (!has1 && !has2)
                    yield break;

                yield return (
                    has1 ? e1.Current : default,
                    has2 ? e2.Current : default
                );
            }
        }
    }

    extension<T>(IEnumerable<T> enumerable) where T : INumber<T>
    {
        public T Sum() => enumerable.Aggregate(T.Zero, (x, y) => x + y);
    }
}