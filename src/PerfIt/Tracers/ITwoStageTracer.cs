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
        /// <summary>
        /// Starts a trace
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        object Start(IInstrumentationInfo info);

        /// <summary>
        /// finishes a trace
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeTakenMilli">If not supplied, time from the start is used</param>
        /// <param name="correlationId"></param>
        /// <param name="extraContext"></param>
        void Finish(object token, long timeTakenMilli = -1, string correlationId = null, InstrumentationContext extraContext = null);
    }
}
