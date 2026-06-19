using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WordCountServer
{
    class Cache
    {
        private Dictionary<string, CacheEntry> _cache = new Dictionary<string, CacheEntry>();
        private LinkedList<string> _lruList = new LinkedList<string>();
        private int _maxSize;
        private object _globalLock = new object(); //za pristup recniku i LRU listi

        public Cache(int maxSize)
        {
            _maxSize = maxSize;
        }

        public async Task<(bool found, int result)> TryGetOrReserveAsync(string fileName)
        {
            CacheEntry entry;
            bool isNewEntry = false;

            lock (_globalLock)
            {
                if (_cache.TryGetValue(fileName, out entry))
                {
                    
                    isNewEntry = false;
                }
                else
                {
                    //nije u kesu, rezervisemo slot odmah
                    if (_cache.Count >= _maxSize)
                        EvictLRU();

                    entry = new CacheEntry(); // prazan placeholder
                    _cache[fileName] = entry;
                    _lruList.AddLast(fileName);

                    entry.Lock.Wait();

                    Logger.Cache($"placeholder rezervisan za '{fileName}'");
                    isNewEntry = true;
                }
            }

            if (isNewEntry)
                return (false, 0);

            //ovo je van globalnog locka, pa drugi taskovi mogu pristupati razlicitim fajlovima paralelno
            await entry.Lock.WaitAsync();
            try
            {
                lock (_globalLock)
                {
                    _lruList.Remove(fileName);
                    _lruList.AddLast(fileName);
                }
                Logger.Cache($"pogodak za '{fileName}' = {entry.Result}");
                return (true, entry.Result);
            }
            finally
            {
                entry.Lock.Release();
            }
        }

        public void Set(string fileName, int result)
        {
            lock (_globalLock)
            {
                if (_cache.ContainsKey(fileName))
                {
                    _cache[fileName].Result = result;
                    _cache[fileName].IsReady = true;
                    //oslobadjamo lock po fajlu, budi sve taskove koji cekaju na ovaj konkretan fajl
                    _cache[fileName].Lock.Release();
                    Logger.Cache($"rezultat sacuvan za '{fileName}' = {result}");
                }
                else
                {
                    if (_cache.Count >= _maxSize)
                        EvictLRU();
                    _cache[fileName] = new CacheEntry(result);
                    _lruList.AddLast(fileName);
                }
            }
        }

        private void EvictLRU()
        {
            string oldest = _lruList.First.Value;
            _lruList.RemoveFirst();
            _cache.Remove(oldest);
            Logger.Cache($"izbacen '{oldest}' (LRU)");
        }
    }
}