using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Threading
{
    interface IWorkFactory
    {
        Func<Task> GetWork();
    }

    class CustomThreadPool : IDisposable
    {
        private IWorkFactory _workFactory;
        private List<Thread> _threadPool = new List<Thread>();
        private int _total;
        private Stopwatch _stopwatch = new Stopwatch();
        public bool IsWorking { get; private set; }
        private int _executing = 0;

        public CustomThreadPool(IWorkFactory workFactory, int size = 100)
        {
            _workFactory = workFactory;

            for (int i = 0; i < size; i++)
            {
                var thread = new Thread(() => LoopAsync().Wait());
                _threadPool.Add(thread);
            }
        }

        public void Start()
        {
            IsWorking = true;
            _threadPool.ForEach((a) => a.Start());
            _stopwatch.Start();
        }

        private async Task LoopAsync()
        {
            while (IsWorking)
            {
                try
                {
                    var workItem = _workFactory.GetWork();
                    if (workItem == null)
                    {
                        while (_executing > 0)
                        {
                            await Task.Delay(50);
                        }

                        Stop();
                    }
                    else
                    {
                        Interlocked.Increment(ref _executing);
                        try
                        {
                            await workItem();
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _executing);
                        }
                    }
                }
                catch (Exception e) // THIS PATH REALLY SHOULD NEVER HAPPEN !!
                {
                    Trace.WriteLine(e);
                }

                Interlocked.Increment(ref _total);
            }
        }

        public void Stop()
        {
            IsWorking = false;
            _stopwatch.Stop();
        }

        public TimeSpan GetElapsed()
        {
            return _stopwatch.Elapsed;
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch
            {
                // ignore
            }
        }
    }
}
