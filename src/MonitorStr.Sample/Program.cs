using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonitorStr.Sample
{
    class Program
    {
        /// <summary>
        /// cache
        /// </summary>
        private static ConcurrentDictionary<string,string> _cache=new ConcurrentDictionary<string, string>();
        private static int Count;
        static void Main(string[] args)
        {
            Console.WriteLine(UtcTimeUtil.CurrentTimeMillis());
            var keys = Enumerable.Range(1, 200).ToList();

            //mock request
            for (int i = 0; i < 200000; i++)
            {
                Task.Run(() =>
                {
                    var key = keys[new Random().Next(1, 200)].ToString();
                    //if key not in cache
                    if (!_cache.ContainsKey(key))
                    {
                        //lock key with 3000 ms
                        var acquired = MonitorStr.TryEnter(key, 3000);
                        if (acquired)
                        {
                            try
                            {
                                //second query cache if not in cache
                                if (!_cache.ContainsKey(key))
                                {
                                    //mock query database
                                    Thread.Sleep(200);
                                    _cache[key]= key;
                                }
                            }
                            finally
                            {
                                MonitorStr.Exit(key);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"key:{key} lock time out");
                            return;
                        }
                    }
                    Interlocked.Increment(ref Count);
                    if(Count>= 200000) Console.WriteLine(UtcTimeUtil.CurrentTimeMillis());
                });
            }

            Console.ReadLine();
        }
    }
}
