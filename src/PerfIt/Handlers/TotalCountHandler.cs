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

        public TotalCountHandler(string applicationName, PerfItFilterAttribute filter) : base(applicationName, filter)
        {
            _counter = new PerformanceCounter()
            {
                CategoryName = filter.CategoryName,
                CounterName = filter.Name,
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
            // nothing 
        }

        protected override void OnRequestEnding(HttpResponseMessage response, PerfItContext context)
        {
            _counter.Increment();
        }

        protected override CounterCreationData[] DoGetCreationData(PerfItFilterAttribute filter)
        {
            return new []
                       {
                           new CounterCreationData()
                               {
                                   CounterName = _filter.Name,
                                   CounterType = PerformanceCounterType.NumberOfItems32,
                                   CounterHelp = _filter.Description
                               }
                       };
        }

        public override void Dispose()
        {
            base.Dispose();
            if(_counter!=null)
                _counter.Dispose();
        }
    }
}
