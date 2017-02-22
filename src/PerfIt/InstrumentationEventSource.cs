using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    [EventSource(Name = "PerfIt-Instrumentation")]
    public class InstrumentationEventSource : EventSource
    {
        public static readonly InstrumentationEventSource Instance = new InstrumentationEventSource();

        private InstrumentationEventSource()
        {
            
        }

        [Event(1, Level = EventLevel.Informational)]
        public void WriteInstrumentationEvent(string categoryName, string instanceName, long timeTakenMilli, string instrumentationContext = null, string correlationId = null)
        {
            this.WriteEvent(1, categoryName, instanceName, timeTakenMilli, instrumentationContext, correlationId);
        }
    }
}
