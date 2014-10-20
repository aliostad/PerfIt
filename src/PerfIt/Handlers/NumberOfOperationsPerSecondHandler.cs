using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt.Handlers
{
    public class NumberOfOperationsPerSecondHandler : CounterHandlerBase
    {
        private Lazy<PerformanceCounter> _counter;
        private const string TimeTakenTicksKey = "NumberOfOperationsPerSecondHandler_#_StopWatch_#_";

        public NumberOfOperationsPerSecondHandler
            (
            string categoryName,
            string instanceName)
            : base(categoryName, instanceName)
        {
           BuildCounters();
        }

        public override string CounterType
        {
            get { return CounterTypes.NumberOfOperationsPerSecond; }
        }

        protected override void DoOnRequestStarting(IPerfItContext context)
        {
           
        }

        protected override void DoOnRequestEnding(IPerfItContext context)
        {
            
            _counter.Value.Increment();
        }

        protected override void BuildCounters(bool newInstanceName = false)
        {
            _counter = new Lazy<PerformanceCounter>(() =>
            {
                var counter = new PerformanceCounter()
                {
                    CategoryName = _categoryName,
                    CounterName = Name,
                    InstanceName = GetInstanceName(newInstanceName),
                    ReadOnly = false,
                    InstanceLifetime = PerformanceCounterInstanceLifetime.Process
                };
                counter.RawValue = 0;
                return counter;
            });
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            var counterCreationDatas = new CounterCreationData[1];
            counterCreationDatas[0] = new CounterCreationData()
            {
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32,
                CounterName = Name,
                CounterHelp = "# of operations / sec"
            };

            return counterCreationDatas;
        }
    }
}
