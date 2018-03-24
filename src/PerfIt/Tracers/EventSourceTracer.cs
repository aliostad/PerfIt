using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt
{
    public class EventSourceTracer : ITwoStageTracer
    {
        public void Dispose()
        {

        }

        public void Finish(object token, 
            long timeTakenMilli, 
            string correlationId = null, 
            InstrumentationContext extraContext = null)
        {
            var info = (IInstrumentationInfo) token;
            InstrumentationEventSource.Instance.WriteInstrumentationEvent(info.CategoryName,
                info.InstanceName,
                timeTakenMilli,
                correlationId,
                extraContext);
        }

        public object Start(IInstrumentationInfo info)
        {
            return info;
        }
    }
}
