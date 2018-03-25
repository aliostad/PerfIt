using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    /// <summary>
    /// Default emitter for zipkin spans
    /// </summary>
    public class SimpleEmitter : ISpanEmitter
    {
        private ConcurrentBag<IDispatcher> _dispatchers = new ConcurrentBag<IDispatcher>();

        public void Emit(Span span)
        {
            foreach (var d in _dispatchers)
            {
                d.Emit(span);
            }
        }

        /// <summary>
        /// Registers a dispatcher
        /// </summary>
        /// <param name="dispatcher">Dispatcher for the spans. THey are meant to implement batching and buffering their end</param>
        public void RegisterDispatcher(IDispatcher dispatcher)
        {
            _dispatchers.Add(dispatcher);
        }
    }
}
