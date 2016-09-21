using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfIt
{
    public class Constants
    {
        public const string PerfItKey = "_#_PerfIt_#_";
        public const string PerfItPublishErrorsKey = "_#_PerfIt_Publish_Error_#_";
        public const string PerfItContextHasErroredKey = "_#_PerfIt_Has_Error_#_";
        public const string PerfItInstrumentationContextKey = "_#_PerfIt_Instrumentation_Context_#_";
        public const string PerfItInstanceNameKey = "_#_PerfIt_Instance_Name_#_";
        public const string PerfItPublishCounters = "perfit:publishCounters";
        public const string PerfItPublishErrors = "perfit:publishErrors";
        public const string PerfItPublishEvent = "perfit:publishEvent";
        public const string InstrumentationContextKey = "__#_PerfItInstrumentationContext_#__";
        public const double DefaultSamplingRate = 1.0d; // 100% sampling
    }
}
