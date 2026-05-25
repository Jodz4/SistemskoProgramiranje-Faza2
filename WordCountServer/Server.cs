using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace WordCountServer
{
    class Server
    {
        private HttpListener _listener;
        private string _prefix;
        private bool _running;
        private FileSearcher _fileSearcher;
        private RequestQueue _requestQueue;
        private Cache _cache;
        private int _workerCount = 4;

        public Server(string prefix, string rootFolder)
        {
            _prefix = prefix;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _fileSearcher = new FileSearcher(rootFolder);
            _requestQueue = new RequestQueue(10);
            _cache = new Cache(5);
        }

        public void Start()
        {
            _listener.Start();
            _running = true;
            Logger.Info($"server pokrenut na: {_prefix}");

            for (int i = 0; i < _workerCount; i++)
            {
                int workerId = i;
                Task.Run(() => WorkerLoop(workerId));
            }

            Logger.Info($"pokrenuto {_workerCount} worker taskova");

            while (_running)
            {
                HttpListenerContext context = _listener.GetContext();
                _requestQueue.Enqueue(context);
            }
        }

        private async Task WorkerLoop(int workerId)
        {
            Logger.Info($"worker {workerId} spreman");

            while (_running)
            {
                HttpListenerContext context = _requestQueue.Dequeue();

                string rawUrl = context.Request.Url.AbsolutePath;
                string fileName = rawUrl.TrimStart('/');

                if (fileName == "favicon.ico")
                {
                    context.Response.StatusCode = 404;
                    context.Response.OutputStream.Close();
                    continue;
                }

                try
                {
                    Logger.Info($"worker {workerId} obradjuje: {fileName}");

                    string response = await ProcessRequestAsync(fileName);

                    SendResponse(context, response);
                    Logger.Info($"worker {workerId} zavrsio: {fileName}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"worker {workerId} greska pri obradi '{fileName}': {ex.Message}");
                    try
                    {
                        SendResponse(context, $"greska na serveru: {ex.Message}");
                    }
                    catch
                    {
                        // ako ni slanje odgovora ne uspe, nastavljamo dalje
                    }
                }
            }
        }

        private async Task<string> ProcessRequestAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "greska! nije naveden naziv fajla";

            if (_cache.TryGetOrReserve(fileName, out int cachedResult))
            {
                if (cachedResult == -1)
                    return $"greska! fajl '{fileName}' nije pronadjen";
                if (cachedResult == 0)
                    return $"fajl '{fileName}' ne sadrzi reci sa vise suglasnika nego samoglasnika";
                return $"fajl: {fileName}\nbroj reci (iz kesa): {cachedResult}";
            }

            string fullPath = await _fileSearcher.FindFileAsync(fileName);

            if (fullPath == null)
            {
                _cache.Set(fileName, -1);
                return $"greska! fajl '{fileName}' nije pronadjen";
            }

            string content = await _fileSearcher.ReadFileAsync(fullPath);

            int count = WordCounter.CountWords(content);
            _cache.Set(fileName, count);

            if (count == 0)
                return $"fajl '{fileName}' ne sadrzi reci sa vise suglasnika nego samoglasnika";

            return $"fajl: {fileName}\nputanja: {fullPath}\nbroj reci: {count}";
        }

        public void Stop()
        {
            _running = false;
            _listener.Stop();
        }

        private void SendResponse(HttpListenerContext context, string message)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = 200;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}