using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    public class SpanEmitHub : IDisposable, IWorkFactory
    {
        private const int DefaultMaxBatchSize = 100;

        public static readonly SpanEmitHub Instance = new SpanEmitHub();

        private readonly ConcurrentQueue<Span> _queue = new ConcurrentQueue<Span>();
        private readonly List<IEmitter> _emitters = new List<IEmitter>();
        private CustomThreadPool _threadPool;
        private DoublyIncreasingTimeInterval _timeInterval = new DoublyIncreasingTimeInterval(
            TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(500), 4);

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

        public void Emit(Span span)
        {
            if(_accepting)
                _queue.Enqueue(span);
        }

        public void RegisterEmitter(IEmitter emitter)
        {
            _emitters.Add(emitter);

            if (_threadPool == null)
            {
                _threadPool = new CustomThreadPool(this, 10);
                _threadPool.Start();
            }
        }

        public void Dispose()
        {
            _threadPool?.Dispose();
        }

        public void Close()
        {
            Dispose();
        }

        public void ClearEmitters()
        {
            _emitters.Clear();    
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
                return () => Task.Delay(_timeInterval.Next());
            }

            _timeInterval.Reset();

            return async () =>
            {
                foreach (var emitter in _emitters)
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
