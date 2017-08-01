using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    public class ConsoleDispatcher : IDispatcher
    {
        public Task EmitBatchAsync(IEnumerable<Span> spans)
        {
            Console.WriteLine($"Received {spans.Count()} spans...");
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            // none
        }
    }
}
