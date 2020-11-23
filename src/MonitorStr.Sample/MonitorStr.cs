using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MonitorStr.Sample
{
    /**
	 * 描述：
	 * 
	 * Author：xuejiaming
	 * Created: 2020/11/23 11:06:27
	 **/
    public class MonitorStr
    {
        private MonitorStr() { }
        private static ConcurrentDictionary<string, MonitorStrEntry> _lockDics = new ConcurrentDictionary<string, MonitorStrEntry>();
        private const int _concurrentCount = 31;
        private static object[] _lockers = new object[_concurrentCount];

        static MonitorStr()
        {
            for (int i = 0; i < _concurrentCount; i++)
            {
                _lockers[i] = new object();
            }
        }
        private static int GetIndex(string key)
        {
            return Math.Abs(key.GetHashCode() % _concurrentCount);
        }
        public static bool TryEnter(string key, int timeoutMillis)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            MonitorStrEntry entry = null;
            var locker = _lockers[GetIndex(key)];
            lock (locker)
            {
                if (!_lockDics.TryGetValue(key, out entry))
                {
                    entry = new MonitorStrEntry();
                    _lockDics[key] = entry;
                }
                entry.Increment();
            }

            var acquired = Monitor.TryEnter(entry, timeoutMillis);
            if (!acquired)
                entry.Decrement();
            return acquired;
        }

        public static void Exit(string key)
        {
            var entry = _lockDics[key];
            Monitor.Exit(entry);
            if (entry.Decrement() == 0)
            {
                var locker = _lockers[GetIndex(key)];
                lock (locker)
                {
                    if (entry.CanRemove())
                    {
                        _lockDics.TryRemove(key, out var v);
                    }
                }
            }
        }
        class MonitorStrEntry
        {
            private int _lockCount;

            public int Increment()
            {
                Interlocked.Increment(ref _lockCount);
                return _lockCount;
            }

            public int Decrement()
            {
                Interlocked.Decrement(ref _lockCount);
                return _lockCount;
            }
            public bool CanRemove()
            {
                return _lockCount == 0;
            }
        }

    }
}
