using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt.Tracers
{
    /// <summary>
    /// Used as a token
    /// </summary>
    public class TimedInstrumentationInfo : InstrumentationInfo
    {
        /// <summary>
        /// When it was created
        /// </summary>
        public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;

        public TimedInstrumentationInfo(IInstrumentationInfo info)
        {
            this.CategoryName = info.CategoryName;
            this.CorrelationIdKey = info.CorrelationIdKey;
            this.Description = info.Description;
            this.InstanceName = info.InstanceName;
            this.Name = info.Name;
            this.RaisePublishErrors = info.RaisePublishErrors;
            this.SamplingRate = info.SamplingRate;

#if NET452

            this.PublishCounters = info.PublishCounters;
            this.Counters = info.Counters;
#endif
        }
    }
}
