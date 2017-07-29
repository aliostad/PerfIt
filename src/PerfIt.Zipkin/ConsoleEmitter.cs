using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    public class ConsoleEmitter : IEmitter
    {
        public Task EmitBatchAsync(IEnumerable<Span> spans)
        {
            Console.WriteLine($"Received {spans.Count()} spans...");
            return Task.FromResult(true);
        }
    }
}
