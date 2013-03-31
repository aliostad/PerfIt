using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt.Handlers
{
    public class TotalCountHandler : CounterHandlerBase
    {

        private readonly PerformanceCounter _counter;

        public TotalCountHandler(string applicationName, string counterName) : base(applicationName, counterName)
        {
            _counter = new PerformanceCounter()
            {
                CategoryName = applicationName,
                CounterName = counterName,
                InstanceName = applicationName,
                ReadOnly = false,
                InstanceLifetime = PerformanceCounterInstanceLifetime.Process
            };
        }

        public override string CounterType
        {
            get { return CounterTypes.TotalNoOfOperations; }
        }

        protected override void OnRequestStarting(HttpRequestMessage request, PerfItContext context)
        {
            throw new NotImplementedException();
        }

        protected override void OnRequestEnding(HttpResponseMessage response, PerfItContext context)
        {
            throw new NotImplementedException();
        }

        protected override CounterCreationData[] DoGetCreationData(PerfItFilterAttribute filter)
        {
            throw new NotImplementedException();
        }
    }
}
