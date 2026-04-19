using System.Collections;

namespace BishUtils;

public class ConcurrentList<T>(IList<T> list) : IList<T>
{
    private readonly Lock _lock = new();
    
    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock) return list.ToList().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        lock (_lock) return ((IEnumerable)list.ToList()).GetEnumerator();
    }

    public void Add(T item)
    {
        lock (_lock) list.Add(item);
    }

    public void Clear()
    {
        lock (_lock) list.Clear();
    }

    public bool Contains(T item)
    {
        lock (_lock) return list.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_lock) list.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        lock (_lock) return list.Remove(item);
    }

    public int Count
    {
        get { lock (_lock) return list.Count; }
    }

    public bool IsReadOnly => false;

    public int IndexOf(T item)
    {
        lock (_lock) return list.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        lock (_lock) list.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        lock (_lock) list.RemoveAt(index);
    }

    public T this[int index]
    {
        get { lock (_lock) return list[index]; }
        set { lock (_lock) list[index] = value; }
    }
}

public static class ConcurrentListHelper
{
    extension<T>(IEnumerable<T> source)
    {
        public ConcurrentList<T> ToConcurrentList() => new(source switch
        {
            IList<T> list => list,
            _ => source.ToList()
        });
    }
}