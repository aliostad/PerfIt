using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    /// <summary>
    /// Outputs spans to .NET Trace
    /// </summary>
    public class TraceDispatcher : IDispatcher
    {
        public void Dispose()
        {
            // none
        }

        public void Emit(Span span)
        {
            Trace.WriteLine($"Received {span.Name} span... => {span.ToString()}");
        }
    }
}
