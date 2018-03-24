using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    /// <summary>
    /// Tracers (such as zipkin tracer) hook into the instrumentation process and emit traces
    /// </summary>
    public interface ITwoStageTracer : IDisposable
    {
        object Start(IInstrumentationInfo info);

        void Finish(object token, long timeTakenMilli, string correlationId = null, InstrumentationContext extraContext = null);
    }
}
