using System.Collections.Generic;

namespace Eshopworld.Telemetry
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Contains extensions methods to facilitate working with Collections.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Adds an Rx subscription to a <see cref="ConcurrentDictionary{Type, IDisposable}"/> that holds <see cref="Type"/> based key subscriptions.
        ///     If a previously typed subscription already exists on the <see cref="ConcurrentDictionary{Type, IDisposable}"/> it will Dispose it (unsubscribe)
        ///     first, remove it and then add the new one with the same key.
        /// </summary>
        /// <param name="dictionary">The Typed <see cref="ConcurrentDictionary{Type, IDisposable}"/> that we want to add the subscription to.</param>
        /// <param name="type">The <see cref="Type"/> key for the Dictionary that we are adding the subscription to.</param>
        /// <param name="subscription">The subscription that we want to add to the <see cref="ConcurrentDictionary{Type, IDisposable}"/>.</param>
        public static void AddSubscription(this ConcurrentDictionary<Type, IDisposable> dictionary, Type type, IDisposable subscription)
        {
            if (dictionary.TryGetValue(type, out IDisposable oldSub))
            {
                oldSub.Dispose();
                if (!dictionary.TryUpdate(type, subscription, oldSub))
                {
                    throw new InvalidOperationException($"Failed updating the dictionary for the type {type.FullName}");
                }
            }
            else
            {
                dictionary.TryAdd(type, subscription);
            }
        }

        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> valueFactory)
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, valueFactory());
            
            return dictionary[key];
        }
    }
}
