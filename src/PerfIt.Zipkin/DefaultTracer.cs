using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Criteo.Profiling.Tracing.Tracers.Zipkin;
using Trace = Criteo.Profiling.Tracing.Trace;

namespace PerfIt.Zipkin
{
    public class DefaultTracer : ITwoStageTracer
    {
        public void Finish(object token, string instrumentationContext = null)
        {
            if(token == null)
                throw new ArgumentNullException(nameof(token));
            var tpl = (Tuple<IInstrumentationInfo, Span, Stopwatch>) token;
            tpl.Item2.SetAsComplete(DateTime.Now);
            ZipkinEventSource.Instance.WriteSpan(
                tpl.Item2, null, instrumentationContext);
            OnFinishing(tpl.Item2);
            
            SpanEmitHub.Instance.Emit(tpl.Item2);         
        }

        public object Start(IInstrumentationInfo info)
        {
            var trace = Trace.Current;
            var newTrace = trace == null ? Trace.Create() : trace.Child();
            Trace.Current = newTrace;
            var span = new Span(newTrace.CurrentSpan, DateTime.Now)
            {
                ServiceName = info.CategoryName,
                Name = info.InstanceName
            };

            OnStarting(span);
            
            return new Tuple<IInstrumentationInfo, Span, Stopwatch>(info, span, Stopwatch.StartNew());
        }

        protected virtual void OnStarting(Span span)
        {
            // none
        }

        protected virtual void OnFinishing(Span span)
        {
            // none
        }

    }

    public class ServerTracer : DefaultTracer
    {
        protected override void OnStarting(Span span)
        {
            base.OnStarting(span);
            span.Annotations.Add(new ZipkinAnnotation(DateTime.Now, "sr"));
        }

        protected override void OnFinishing(Span span)
        {
            base.OnFinishing(span);
            span.Annotations.Add(new ZipkinAnnotation(DateTime.Now, "ss"));
        }
    }

    public class ClientTracer : DefaultTracer
    {
        protected override void OnStarting(Span span)
        {
            base.OnStarting(span);
            span.Annotations.Add(new ZipkinAnnotation(DateTime.Now, "cs"));
        }

        protected override void OnFinishing(Span span)
        {
            base.OnFinishing(span);
            span.Annotations.Add(new ZipkinAnnotation(DateTime.Now, "cr"));
        }
    }

}
