namespace BishUtils;

public static class ListHelper
{
    extension<T>(IList<T> list)
    {
        public int FindIndex(Predicate<T> predicate) => list.FindIndex(0, predicate);
        
        public int FindIndex(int start, Predicate<T> predicate)
        {
            for (var i = start; i < list.Count; i++)
                if (predicate(list[i]))
                    return i;
            return -1;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items) list.Add(item);
        }

        public void RemoveRange(int index, int count)
        {
            for (var i = index + count - 1; i >= index; i--) list.RemoveAt(i);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            foreach (var item in items.Reverse()) list.Insert(index, item);
        }

        public IList<T> Slice(int start, int? end = null)
        {
            var count = list.Count;
            var actualStart = start < 0 ? count + start : start;
            var actualEnd = end.HasValue ? end.Value < 0 ? count + end.Value : end.Value : count;
            var sliceLength = actualEnd - actualStart;
            return list.Skip(actualStart).Take(sliceLength).ToList().ToConcurrentList();
        }
    }
}