using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    public class InstrumentationToken
    {
        public Tuple<IEnumerable<PerfitHandlerContext>, Dictionary<string, object>> Contexts { get; set; }

        public Stopwatch Kronometer { get; set; }

        public double SamplingRate { get; set; }

        public string CorrelationId { get; set; }
    }
}
