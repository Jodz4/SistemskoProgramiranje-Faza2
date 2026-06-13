using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace WordCountServer
{
    class RequestQueue
    {
        private Queue<HttpListenerContext> _queue = new Queue<HttpListenerContext>();
        private int _maxSize;
        private object _lock = new object();

        public RequestQueue(int maxSize)
        {
            _maxSize = maxSize;
        }

        public bool TryEnqueue(HttpListenerContext context)
        {
            lock (_lock)
            {
                if (_queue.Count >= _maxSize)
                {
                    Logger.Warning("red je pun, odbijam zahtev (HTTP 503)");
                    return false;
                }

                _queue.Enqueue(context);
                Monitor.Pulse(_lock);
                return true;
            }
        }

        public HttpListenerContext Dequeue(CancellationToken token)
        {
            lock (_lock)
            {
                while (_queue.Count == 0)
                {
                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException();
                    Monitor.Wait(_lock, 500); //timeout od 500ms
                }
                HttpListenerContext context = _queue.Dequeue();
                Monitor.Pulse(_lock);
                return context;
            }
        }
    }
}