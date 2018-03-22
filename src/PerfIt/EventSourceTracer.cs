using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt
{
    public class EventSourceTracer : ITwoStageTracer
    {
        public void Finish(object token, 
            long timeTakenMilli, 
            string correlationId = null, 
            string instrumentationContext = null, 
            ExtraContext extraContext = null)
        {
            var info = (IInstrumentationInfo) token;
            InstrumentationEventSource.Instance.WriteInstrumentationEvent(info.CategoryName,
                info.InstanceName,
                timeTakenMilli,
                instrumentationContext,
                correlationId,
                extraContext);
        }

        public object Start(IInstrumentationInfo info)
        {
            return info;
        }
    }
}
