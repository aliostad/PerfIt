using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    /// <summary>
    /// Emits a batch of spans to a destination (media/queue/storage)
    /// </summary>
    public interface IDispatcher : IDisposable
    {
        Task EmitBatchAsync(IEnumerable<Span> spans);
    }
}
