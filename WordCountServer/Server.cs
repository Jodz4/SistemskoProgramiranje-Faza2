using System;
using System.Collections.Generic;
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
        private SemaphoreSlim _semaphore;
        private CancellationTokenSource _cts;
        private List<Task> _workerTasks;

        public Server(string prefix, string rootFolder)
        {
            _prefix = prefix;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _fileSearcher = new FileSearcher(rootFolder);
            _requestQueue = new RequestQueue(10);
            _cache = new Cache(5);
            _semaphore = new SemaphoreSlim(_workerCount, _workerCount);
            _cts = new CancellationTokenSource();
            _workerTasks = new List<Task>();
        }

        public void Start()
        {
            _listener.Start();
            _running = true;
            Logger.Info($"server pokrenut na: {_prefix}");

            for (int i = 0; i < _workerCount; i++)
            {
                int workerId = i;
                Task t = Task.Run(() => WorkerLoop(workerId, _cts.Token));
                _workerTasks.Add(t);
            }

            Logger.Info($"pokrenuto {_workerCount} worker taskova");

            while (_running)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    _requestQueue.Enqueue(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
            }
        }

        public void Stop()
        {
            Logger.Info("gasenje servera...");
            _running = false;
            _cts.Cancel();
            _listener.Stop();
            Logger.Info("cekam da worker taskovi zavrse...");
            Task.WaitAll(_workerTasks.ToArray());
            Logger.Info("svi worker taskovi zavrseni");
        }

        private async Task WorkerLoop(int workerId, CancellationToken token)
        {
            Logger.Info($"worker {workerId} spreman");

            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context;

                try
                {
                    context = _requestQueue.Dequeue(token);
                }
                catch (OperationCanceledException)
                {
                    Logger.Info($"worker {workerId} prima signal za gasenje");
                    break;
                }

                string rawUrl = context.Request.Url.AbsolutePath;
                string fileName = rawUrl.TrimStart('/');

                if (fileName == "favicon.ico")
                {
                    context.Response.StatusCode = 404;
                    context.Response.OutputStream.Close();
                    continue;
                }

                await _semaphore.WaitAsync(token);

                try
                {
                    Logger.Info($"worker {workerId} obradjuje: {fileName}");
                    string response = await ProcessRequestAsync(fileName);
                    SendResponse(context, response);
                    Logger.Info($"worker {workerId} zavrsio: {fileName}");
                }
                catch (OperationCanceledException)
                {
                    Logger.Info($"worker {workerId} prekinut tokom obrade");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"worker {workerId} greska: {ex.Message}");
                    try { SendResponse(context, $"greska na serveru: {ex.Message}"); }
                    catch { }
                }
                finally
                {
                    _semaphore.Release();
                    Logger.Info($"worker {workerId} oslobodio semaphore");
                }
            }

            Logger.Info($"worker {workerId} ugasen");
        }


        private async Task<string> ProcessRequestAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "greska! nije naveden naziv fajla";

            var (found, cachedResult) = await _cache.TryGetOrReserveAsync(fileName);

            if (found)
            {
                if (cachedResult == -1)
                    return $"greska! fajl '{fileName}' nije pronadjen";
                if (cachedResult == 0)
                    return $"fajl '{fileName}' ne sadrzi reci sa vise suglasnika nego samoglasnika";
                return $"fajl: {fileName}\nbroj reci (iz kesa): {cachedResult}";
            }

            //await sa ContinueWith lancem za obradu fajla, await ceka na ceo lanac da zavrsi
            return await _fileSearcher.FindFileAsync(fileName)

                .ContinueWith(findTask =>
                {
                    //Thread.Sleep(3000); //odkomentarisi za testiranje
                    string fullPath = findTask.Result;

                    if (fullPath == null)
                    {
                        _cache.Set(fileName, -1);
                        return Task.FromResult("__NOT_FOUND__");
                    }

                    return _fileSearcher.ReadFileAsync(fullPath)
                        .ContinueWith(readTask =>
                        {
                            string content = readTask.Result;
                            int count = WordCounter.CountWords(content);
                            _cache.Set(fileName, count);
                            return $"{fullPath}|{count}";
                        });
                })
                .Unwrap()

                .ContinueWith(resultTask =>
                {
                    string result = resultTask.Result;

                    if (result == "__NOT_FOUND__")
                        return $"greska! fajl '{fileName}' nije pronadjen";

                    string[] parts = result.Split('|');
                    string fullPath = parts[0];
                    int count = int.Parse(parts[1]);

                    if (count == 0)
                        return $"fajl '{fileName}' ne sadrzi reci sa vise suglasnika nego samoglasnika";

                    return $"fajl: {fileName}\nputanja: {fullPath}\nbroj reci: {count}";
                });
        }

        private void SendResponse(HttpListenerContext context, string message)
        {
            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch (ObjectDisposedException)
            {
                //listener je ugasen tokom slanja odgovora, ignorisemo
            }
        }
    }
}