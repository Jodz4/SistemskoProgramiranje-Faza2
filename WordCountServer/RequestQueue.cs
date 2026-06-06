using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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

        public void Enqueue(HttpListenerContext context)
        {
            lock (_lock)
            {
                while (_queue.Count >= _maxSize)
                {
                    Logger.Warning("red je pun, sacekaj!");
                    Monitor.Wait(_lock);
                }

                _queue.Enqueue(context);
                Monitor.Pulse(_lock);
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
