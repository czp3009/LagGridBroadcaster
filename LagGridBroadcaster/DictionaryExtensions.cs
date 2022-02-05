using System.Collections.Generic;

namespace LagGridBroadcaster
{
    internal static class DictionaryExtensions
    {
        internal static void AddOrUpdateList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key,
            TValue value)
        {
            if (dictionary.TryGetValue(key, out var values))
            {
                values.Add(value);
            }
            else
            {
                dictionary.Add(key, new List<TValue> { value });
            }
        }
    }
}