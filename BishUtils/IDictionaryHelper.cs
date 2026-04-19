namespace BishUtils;

public static class DictionaryHelper
{
    extension<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
    {
        public TValue? GetValueOrDefault(TKey key) => dictionary.TryGetValue(key, out var value) ? value : default;
    }
}