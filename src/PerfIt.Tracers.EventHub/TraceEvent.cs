using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt.Tracers.EventHub
{
    public class TraceEvent
    {
        /// <summary>
        /// Instance name of the event
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// Description of the counter. Will be published to counter metadata visible in Perfmon.
        /// </summary>

        public string CategoryName { get; set; }

        public string Text1 { get; set; }

        public string Text2 { get; set; }

        public int Numeric { get; set; }

        public decimal Decimal { get; set; }

        public long TimeTakenMilli { get; set; }

        public string CorrelationId { get; set; }

        public string InstrumentationContext { get; set; }
    }
}
