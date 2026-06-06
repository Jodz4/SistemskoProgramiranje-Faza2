namespace WordCountServer
{
    class CacheEntry
    {
        public int Result { get; set; }
        public bool IsReady { get; set; }

        //svaki entry ima svoj SemaphoreSlim(1,1)
        //ovo omogucava da vise taskova ceka na RAZLICITE fajlove paralelno
        //SemaphoreSlim(1,1), max 1 task moze biti unutar lock-a
        public System.Threading.SemaphoreSlim Lock { get; } = new System.Threading.SemaphoreSlim(1, 1);

        public CacheEntry(int result)
        {
            Result = result;
            IsReady = true;
        }

        public CacheEntry()
        {
            IsReady = false;
        }
    }
}