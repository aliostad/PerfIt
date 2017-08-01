using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    /// <summary>
    /// Central hub for all tracers to send their spans to
    /// </summary>
    public interface ISpanEmitter
    {
        /// <summary>
        /// Thread-safe. It enqueues for emission.
        /// </summary>
        /// <param name="span"></param>
        void Emit(Span span);

        /// <summary>
        /// Currently not thread-safe. Must be called at the startup of the application
        /// </summary>
        /// <param name="dispatcher"></param>
        void RegisterDispatcher(IDispatcher dispatcher);
    }
}
