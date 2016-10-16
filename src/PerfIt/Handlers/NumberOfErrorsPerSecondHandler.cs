using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt.Handlers
{
    public class NumberOfErrorsPerSecondHandler : CounterHandlerBase
    {
        private Lazy<PerformanceCounter> _counter;
        private const string TimeTakenTicksKey = "NumberOfOperationsPerErrorsHandler_#_StopWatch_#_";

        public NumberOfErrorsPerSecondHandler(string categoryName, string instanceName)
            : base(categoryName, instanceName)
        {
           BuildCounters();
        }

        public override string CounterType
        {
            get { return CounterTypes.NumberOfErrorsPerSecond; }
        }

        protected override void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context)
        {
        }

        protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
        {
            if (!contextBag.ContainsKey(Constants.PerfItContextHasErroredKey)) return;
            _counter.Value.Increment();
        }

        protected override void BuildCounters(bool newInstanceName = false)
        {
            _counter = new Lazy<PerformanceCounter>(() =>
            {
                var counter = new PerformanceCounter()
                {
                    CategoryName = CategoryName,
                    CounterName = Name,
                    InstanceName = GetInstanceName(newInstanceName),
                    ReadOnly = false,
                    InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
                    RawValue = 0
                };
                return counter;
            });
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            var counterCreationDatas = new CounterCreationData[1];
            counterCreationDatas[0] = new CounterCreationData
            {
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32,
                CounterName = Name,
                CounterHelp = "# of error operations / sec"
            };

            return counterCreationDatas;
        }
    }
}
