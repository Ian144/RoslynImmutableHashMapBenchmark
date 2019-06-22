using System;
using System.Threading;
using Contract = System.Diagnostics.Contracts.Contract;



namespace RoslynImmutableHashMapConsoleApp
{
    internal static class ImmutableHashMapExtensions
    {
        public static TValue GetOrAdd<TKey, TValue, TArg>(ref ImmutableHashMap<TKey, TValue> location, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            var map = Volatile.Read(ref location);

            if (map.TryGetValue(key, out var existingValue))
            {
                return existingValue;
            }

            TValue newValue = valueFactory(key, factoryArgument);

            do
            {
                var augmentedMap = map.Add(key, newValue);

                var replacedMap = Interlocked.CompareExchange(ref location, augmentedMap, map);
                if (replacedMap == map)
                {
                    return newValue;
                }

                map = replacedMap;
            }
            while (!map.TryGetValue(key, out existingValue));

            return existingValue;
        }
    }
}
