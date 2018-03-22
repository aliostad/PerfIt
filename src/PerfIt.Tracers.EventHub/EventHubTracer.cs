using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using Psyfon;
using System;
using System.Collections.Generic;
using System.Text;

namespace PerfIt.Tracers.EventHub
{
    /// <summary>
    /// A tracer that pushes instrumentation events to EventHub
    /// </summary>
    public class EventHubTracer : ITwoStageTracer
    {
        private readonly IEventDispatcher _dispatcher;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="dispatcher">an event dispatcher</param>
        public EventHubTracer(IEventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Finish(object token, long timeTakenMilli, string correlationId = null, 
            InstrumentationContext extraContext = null)
        {
            var info = (IInstrumentationInfo) token;
            var so = JsonConvert.SerializeObject(new TraceEvent()
            {
                CategoryName = info.CategoryName,
                InstanceName = info.InstanceName,
                CorrelationId = correlationId,
                Text1 = extraContext?.Text1,
                Text2 = extraContext?.Text2,
                Numeric = extraContext?.Numeric ?? 0,
                Decimal = extraContext?.Decimal ?? 0,
                TimeTakenMilli = timeTakenMilli
            });

            _dispatcher.Add(new EventData(Encoding.UTF8.GetBytes(so)));
        }

        public object Start(IInstrumentationInfo info)
        {
            return info;
        }
    }
}
