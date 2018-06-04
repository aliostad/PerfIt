using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt.Tracers.EventHub
{
    public class TraceEvent
    {
        public TraceEvent()
        {
            MachineName = Environment.MachineName;
            EventDate = DateTimeOffset.Now;
        }

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

        public double Decimal { get; set; }

        public long TimeTakenMilli { get; set; }

        public string CorrelationId { get; set; }

        public DateTimeOffset EventDate { get; set; }

        public string MachineName { get; set; }
    }
}
