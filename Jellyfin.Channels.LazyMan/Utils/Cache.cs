using System.Collections.Concurrent;
using System.Timers;

namespace Jellyfin.Channels.LazyMan.Utils
{
    public class CacheItem<T>
    {
        private readonly string _key;
        public T Value { get; }
        private readonly Timer _timer;
        private readonly ConcurrentDictionary<string, CacheItem<T>> _cacheRef;

        public CacheItem(string key, T value, double expireMs, ConcurrentDictionary<string, CacheItem<T>> cacheRef)
        {
            _cacheRef = cacheRef;
            _key = key;
            Value = value;

            _timer = new Timer(expireMs);
            _timer.Elapsed += Timer_Expire;
            _timer.Start();
        }

        private void Timer_Expire(object sender, ElapsedEventArgs e)
        {
            _timer.Elapsed -= Timer_Expire;
            _cacheRef.TryRemove(_key, out _);

        }
    }
}