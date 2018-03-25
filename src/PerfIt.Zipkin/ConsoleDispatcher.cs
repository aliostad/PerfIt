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
        public void Emit(Span span)
        {
            Console.WriteLine($"Received {span.Name} span...");
        }

        public void Dispose()
        {
            // none
        }
    }
}
