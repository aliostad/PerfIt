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

        public void WriteSpan(Span span, string correlationId = null, InstrumentationContext context = null )
        {
            var ctx = context ?? new InstrumentationContext();
            Write(span.ServiceName,
                span.Name,
                span.Duration.HasValue ? (span.Duration.Value.Ticks / 10) : 0,
                span.SpanState.TraceId,
                span.SpanState.SpanId,
                span.SpanState.ParentSpanId,
                correlationId,
                ctx.Text1,
                ctx.Text2,
                ctx.Numeric,
                ctx.Decimal);
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
        [Event(42, Level = EventLevel.Informational)]
        public void Write(
            string categoryName, 
            string instanceName, 
            long timeTakenMicro,  
            long traceId,
            long spanId,
            long? parentId,
            string correlationId,
            string text1 = null,
            string text2 = null,
            int numeric = 0,
            decimal Decimal = 0)
        {
            this.WriteEvent(42, 
                categoryName, 
                instanceName, 
                timeTakenMicro, 
                correlationId,
                traceId,
                spanId,
                parentId,
                text1,
                text2,
                numeric,
                Decimal);
        }
    }
}
