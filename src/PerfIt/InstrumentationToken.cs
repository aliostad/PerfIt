using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    public class InstrumentationToken
    {
        public Dictionary<string, object> Contexts { get; set; }

        public Stopwatch Kronometer { get; set; }

        public double SamplingRate { get; set; }

        public object CorrelationId { get; set; }

        public Dictionary<string, object> TracerContexts { get; set; }
    }
}
