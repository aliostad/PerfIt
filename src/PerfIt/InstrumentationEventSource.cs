using System.Diagnostics.Tracing;

namespace PerfIt
{
    /// <summary>
    /// Represents an InstrumentationEventSource.
    /// </summary>
    [EventSource(Name = "PerfIt-Instrumentation")]
    public class InstrumentationEventSource : EventSource
    {
        /// <summary>
        /// Provides a default Instance of the <see cref="EventSource"/>.
        /// </summary>
        public static readonly InstrumentationEventSource Instance = new InstrumentationEventSource();

        private InstrumentationEventSource()
        {
        }

        /// <summary>
        /// Provides a default WriteInstrumentationEvent <see cref="PublishInstrumentationDelegate"/>.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="instanceName"></param>
        /// <param name="timeTakenMilli"></param>
        /// <param name="instrumentationContext"></param>
        [Event(1, Level = EventLevel.Informational)]
        public void WriteInstrumentationEvent(string categoryName, string instanceName,
            double timeTakenMilli, string instrumentationContext = null)
        {
            this.WriteEvent(1, categoryName, instanceName, timeTakenMilli, instrumentationContext);
        }
    }
}
