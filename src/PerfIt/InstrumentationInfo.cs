using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public class InstrumentationInfo : IInstrumentationInfo
    {
        private bool _publishCounters = true;
        private bool _publishEvent = true;

        public string InstanceName { get; set; }

        public string Description { get; set; }

        public string[] Counters { get; set; }

        public string CategoryName { get; set; }

        /// <summary>
        /// Whether publish windows performance counters
        /// </summary>
        public bool PublishCounters
        {
            get
            {
                return _publishCounters;
            }
            set
            {
                _publishCounters = value;
            }
        }

        /// <summary>
        /// Whether throw exceptions if publishing counters/events failed or only write to trace
        /// </summary>
        public bool RaisePublishErrors { get; set; }

        /// <summary>
        /// Whether to publish ETW events
        /// </summary>
        public bool PublishEvent
        {
            get
            {
                return _publishEvent;
            }
            set
            {
                _publishEvent = value;
            }
        }

        /// <summary>
        /// A value between 0.0 and 1.0 as the proportion of calls to be sampled.
        /// Useful if you are generating a lot of ETW events
        /// </summary>
        public double SamplingRate { get; set; }

        public string CorrelationIdKey { get; set; }
    }
}
