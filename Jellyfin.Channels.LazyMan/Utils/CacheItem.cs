using System.Collections.Concurrent;
using System.Timers;

namespace Jellyfin.Channels.LazyMan.Utils
{
    /// <summary>
    /// Item cache.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    public class CacheItem<T>
    {
        private readonly string _key;
        private readonly Timer _timer;
        private readonly ConcurrentDictionary<string, CacheItem<T>> _cacheRef;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The item value.</param>
        /// <param name="expireMs">The cache expire time.</param>
        /// <param name="cacheRef">The cache reference.</param>
        public CacheItem(string key, T value, double expireMs, ConcurrentDictionary<string, CacheItem<T>> cacheRef)
        {
            _cacheRef = cacheRef;
            _key = key;
            Value = value;

            _timer = new Timer(expireMs);
            _timer.Elapsed += Timer_Expire;
            _timer.Start();
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public T Value { get; }

        private void Timer_Expire(object sender, ElapsedEventArgs e)
        {
            _timer.Elapsed -= Timer_Expire;
            _cacheRef.TryRemove(_key, out _);
        }
    }
}