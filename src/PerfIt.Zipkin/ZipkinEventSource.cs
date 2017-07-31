using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;

namespace PerfIt.Zipkin
{
    [EventSource(Name = "Zipkin-Instrumentation")]
    public class ZipkinEventSource : EventSource
    {
        public static readonly ZipkinEventSource Instance = new ZipkinEventSource();
        private const int TicksPerMicrosecond = 10;

        private ZipkinEventSource()
        {

        }

        public void WriteSpan(Span span, string correlationId = null, string instrumentationContext = null)
        {
            Write(span.ServiceName,
                span.Name,
                span.Duration.HasValue ? (span.Duration.Value.Ticks / 10) : 0,
                span.SpanState.TraceId,
                span.SpanState.SpanId,
                span.SpanState.ParentSpanId,
                correlationId,
                instrumentationContext);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="categoryName">Name of the service</param>
        /// <param name="instanceName">Name of the span</param>
        /// <param name="timeTakenMicro">In microseconds</param>
        /// <param name="traceId"></param>
        /// <param name="spanId"></param>
        /// <param name="parentId"></param>
        /// <param name="correlationId"></param>
        /// <param name="instrumentationContext"></param>
        [Event(42, Level = EventLevel.Informational)]
        public void Write(
            string categoryName, 
            string instanceName, 
            long timeTakenMicro,  
            long traceId,
            long spanId,
            long? parentId,
            string correlationId,
            string instrumentationContext = null)
        {
            this.WriteEvent(42, 
                categoryName, 
                instanceName, 
                timeTakenMicro, 
                correlationId,
                traceId,
                spanId,
                parentId,
                instrumentationContext
                );
        }
    }
}
