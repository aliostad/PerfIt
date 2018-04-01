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

        [Event(12, Level = EventLevel.Informational)]
        public void WriteInstrumentationEvent(string categoryName, 
            string instanceName,
            long timeTakenMilli, 
            string correlationId = null,
            string text1 = null,
            string text2 = null,
            int numeric = 0,
            double decima1 = 0)
        {
            WriteEvent(12, categoryName ?? "NoCategory", instanceName ?? "NoInstance", timeTakenMilli,  
                correlationId ??  string.Empty, text1 ?? string.Empty, text2 ?? string.Empty, numeric, decima1);
        }       
    }

    public static class InstrumentationExtensions
    {
        public static void WriteInstrumentationEvent(this InstrumentationEventSource source, string categoryName, string instanceName, long timeTakenMilli, string correlationId = null, InstrumentationContext extraContext = null)
        {
            source.WriteInstrumentationEvent(categoryName ?? "NoCategory", instanceName ?? "NoInstance", timeTakenMilli,
                correlationId == null ? string.Empty : correlationId, extraContext?.Text1, extraContext?.Text2 ?? string.Empty, extraContext?.Numeric ?? 0, extraContext?.Decimal ?? 0);
        }
    }
}
