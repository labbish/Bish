namespace BishUtils;

public static class EnumerableHelper
{
    extension<T>(IEnumerable<T> enumerable)
    {
        public IEnumerable<(T, int)> Enumerate() => enumerable.Select((x, i) => (x, i));
    }
}