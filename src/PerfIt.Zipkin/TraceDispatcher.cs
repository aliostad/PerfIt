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
        public Task EmitBatchAsync(IEnumerable<Span> spans)
        {
            Trace.WriteLine($"Received {spans.Count()} spans...");
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            // none
        }
    }
}
