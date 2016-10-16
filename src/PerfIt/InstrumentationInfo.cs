using System;
using System.Linq;

namespace PerfIt
{
    /// <summary>
    /// Provides InstrumentationInfo to the project.
    /// </summary>
    public class InstrumentationInfo : IInstrumentationInfo
    {
        public string InstanceName { get; set; }

        public string Description { get; set; }

        public string[] Counters { get; set; }

        public string CategoryName { get; set; }

        /// <summary>
        /// Whether publish windows performance counters
        /// </summary>
        public bool PublishCounters { get; set; }

        /// <summary>
        /// Whether throw exceptions if publishing counters/events failed or only write to trace
        /// </summary>
        public bool RaisePublishErrors { get; set; }

        /// <summary>
        /// Whether to publish ETW events
        /// </summary>
        public bool PublishEvent { get; set; }

        private double _sampleRate;

        /// <summary>
        /// A value between 0.0 and 1.0 as the proportion of calls to be sampled.
        /// Useful if you are generating a lot of ETW events
        /// </summary>
        public double SamplingRate
        {
            get { return _sampleRate; }
            set { _sampleRate = Math.Max(0d, Math.Min(1d, value)); }
        }

        /// <summary>
        /// Returns whether RequiresInstrumentationContext.
        /// </summary>
        public bool RequiresInstrumentationContext
        {
            get { return PublishEvent || PublishCounters || RaisePublishErrors; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public InstrumentationInfo()
        {
            Description = string.Empty;
            Counters = CounterTypes.StandardCounters.ToArray();
            SamplingRate = Constants.DefaultSamplingRate;
            PublishCounters = true;
            RaisePublishErrors = false;
            PublishEvent = true;
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="other"></param>
        public InstrumentationInfo(IInstrumentationInfo other)
        {
            CategoryName = other.CategoryName;
            Counters = other.Counters.ToArray();
            Description = other.Description;
            InstanceName = other.InstanceName;
            PublishCounters = other.PublishCounters;
            PublishEvent = other.PublishEvent;
            RaisePublishErrors = other.RaisePublishErrors;
            SamplingRate = other.SamplingRate;
        }
    }
}
