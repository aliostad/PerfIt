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
        public void WriteInstrumentationEvent(string categoryName, 
            string instanceName,
            long timeTakenMilli, 
            string correlationId = null,
            string text1 = null,
            string text2 = null,
            int numeric = 0,
            decimal decima1 = 0)
        {
            this.WriteEvent(1, categoryName ?? "NoCategory", instanceName ?? "NoInstance", timeTakenMilli,  
                correlationId == null ? string.Empty : correlationId, text1 ?? string.Empty, text2 ?? string.Empty, numeric, decima1);
        }

        public void WriteInstrumentationEvent(string categoryName, string instanceName, long timeTakenMilli, string correlationId = null, InstrumentationContext extraContext = null)
        {
            this.WriteEvent(1, categoryName ?? "NoCategory", instanceName ?? "NoInstance", timeTakenMilli, 
                correlationId == null ? string.Empty : correlationId, extraContext?.Text1, extraContext?.Text2, extraContext?.Numeric, extraContext?.Decimal);
        }

        
    }
}
