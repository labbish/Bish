namespace BishUtils;

public static class ListHelper
{
    extension<T>(IList<T> list)
    {
        public int FindIndex(Predicate<T> predicate)
        {
            foreach (var (item, i) in list.Enumerate())
                if (predicate(item))
                    return i;
            return -1;
        }
    }
}