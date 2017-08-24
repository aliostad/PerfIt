using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    public class SpanEmitHub : IDisposable, IWorkFactory, ISpanEmitter
    {
        private const int DefaultMaxBatchSize = 100;

        public static readonly SpanEmitHub Instance = new SpanEmitHub();

        private readonly ManualResetEvent _sync = new ManualResetEvent(false);
        private readonly ConcurrentQueue<Span> _queue = new ConcurrentQueue<Span>();
        private readonly List<IDispatcher> _dispatchers = new List<IDispatcher>();
        private CustomThreadPool _threadPool;

        private DoublyIncreasingIntInterval _batchSizeInterval = new DoublyIncreasingIntInterval(
            10, DefaultMaxBatchSize, 4);

        private bool _accepting = true;

        private SpanEmitHub()
        {
            MaxBatchSize = DefaultMaxBatchSize;
            AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
            {
                Close();
            };
        }

        public int MaxBatchSize { get; set; }

        public int QueueCount
        {
            get { return _queue.Count; }
        }

        public void Emit(Span span)
        {
            if (_accepting)
            {
                _queue.Enqueue(span);
                _sync.Set();
            }
        }

        /// <summary>
        /// Registers a dispatcher. NOTE: If you register a dispatcher, please make sure you call Close/Dispose at the end of your application
        /// </summary>
        /// <param name="dispatcher"></param>
        public void RegisterDispatcher(IDispatcher dispatcher)
        {
            _dispatchers.Add(dispatcher);

            if (_threadPool == null)
            {
                _threadPool = new CustomThreadPool(this, 10);
                _threadPool.Start();
            }
        }

        public void Dispose()
        {
            _threadPool?.Dispose();
            foreach (var emitter in _dispatchers)
            {
                try
                {
                    emitter.Dispose();
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void ClearDispatchers()
        {
            _dispatchers.Clear();    
        }

        public Func<Task> GetWork()
        {
            Span span;
            var spans = new List<Span>();
            for (int i = 0; i < _batchSizeInterval.Next(); i++)
            {
                if (!_queue.TryDequeue(out span))
                    break;
                spans.Add(span);
            }

            if (spans.Count == 0)
            {
                _batchSizeInterval.Reset();
                return () =>
                {
                    _sync.Reset();
                    _sync.WaitOne();
                    return Task.FromResult(false);
                };
            }
           
            return async () =>
            {
                // this approach is simplistic if there are more than one IO-bound emitter
                // but for now it is OK. In case of multiple IO-bound emitters, then we
                // must have a separate queue for each batch per emitter
                foreach (var emitter in _dispatchers)
                {
                    await emitter.EmitBatchAsync(spans);
                }
            };
        }

        private class DoublyIncreasingTimeInterval : IntervalBase<TimeSpan>
        {
            private TimeSpan _increment;

            public DoublyIncreasingTimeInterval(TimeSpan startInterval, TimeSpan maxInterval, int intervalCount)
                : base(startInterval, maxInterval)
            {
                _increment = new TimeSpan((maxInterval.Ticks - startInterval.Ticks) / intervalCount);

            }

            protected override TimeSpan CalculateNext(TimeSpan current)
            {
                return current + _increment;
            }
        }

        private class DoublyIncreasingIntInterval : IntervalBase<int>
        {
            private int _increment;

            public DoublyIncreasingIntInterval(int startInterval, int maxInterval, int intervalCount)
                : base(startInterval, maxInterval)
            {
                _increment = (int) ((maxInterval - startInterval) / intervalCount);

            }

            protected override int CalculateNext(int current)
            {
                return current + _increment;
            }
        }

        private abstract class IntervalBase<T>
            where T : IComparable<T>
        {
            protected readonly T _startInterval;
            protected readonly T _maxInterval;

            private T _current;
            protected abstract T CalculateNext(T current);

            public IntervalBase(T startInterval, T maxInterval)
            {
                _maxInterval = maxInterval;
                _startInterval = startInterval;
                _current = _startInterval;
            }


            // NOTE: returns current and calculates next
            public T Next()
            {

                T toReturn = _current;

                T next = CalculateNext(_current);

                if (next.CompareTo(_startInterval) < 0)
                    throw new InvalidOperationException("Next interval must be equal or greater to start interval.");

                _current = _maxInterval.CompareTo(next) > 0 ? next : _maxInterval;

                return toReturn;
            }

            public T Reset()
            {
                return _current = _startInterval;
            }
        }
    }
}
