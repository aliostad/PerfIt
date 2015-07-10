using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    [EventSource(Name = "PerIt!Instrumentation", Guid = "{010380F8-40A7-45C3-B87B-FD4C6CC8700A}")]
    public class InstrumentationEventSource : EventSource
    {
        public static readonly InstrumentationEventSource Instance = new InstrumentationEventSource();

        private InstrumentationEventSource()
        {
            
        }

        [Event(1, Level = EventLevel.Informational)]
        public void WriteInstrumentationEvent(string categoryName, string instanceName, long timeTakenMilli, string instrumentationContext = null)
        {
            this.WriteEvent(1, categoryName, instanceName, timeTakenMilli, instrumentationContext);
        }
    }
}
