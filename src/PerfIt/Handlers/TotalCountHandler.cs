using System;
using System.Diagnostics;
using System.Net.Http;

namespace PerfIt.Handlers
{
    public class TotalCountHandler : CounterHandlerBase
    {

        private readonly Lazy<PerformanceCounter> _counter;

        public TotalCountHandler
            (
            string categoryName,
            string instanceName,
            PerfItFilterAttribute filter)
            : base(categoryName, instanceName, filter)
        {
            
            _counter = new Lazy<PerformanceCounter>(() =>
            {
                var counter = new PerformanceCounter()
                {
                    CategoryName = categoryName,
                    CounterName = Name,
                    InstanceName = instanceName,
                    ReadOnly = false,
                    InstanceLifetime = PerformanceCounterInstanceLifetime.Process
                };
                counter.RawValue = 0;
                return counter;
            }
              );
             
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
            _counter.Value.Increment();
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            return new []
                       {
                           new CounterCreationData()
                               {
                                   CounterName = Name,
                                   CounterType = PerformanceCounterType.NumberOfItems32,
                                   CounterHelp = _filter.Description
                               }
                       };
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_counter != null && _counter.IsValueCreated)
            {
                _counter.Value.RemoveInstance();
                _counter.Value.Dispose(); 
            }
        }

        
    }
}
