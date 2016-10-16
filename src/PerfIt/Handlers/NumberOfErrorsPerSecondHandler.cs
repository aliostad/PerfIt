using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    /// <summary>
    /// Number of Errors Per Second Counter handler.
    /// </summary>
    public class NumberOfErrorsPerSecondHandler : CounterHandlerBase
    {
        private Lazy<PerformanceCounter> _counter;

        [Obsolete] // TODO: TBD: unnecessary?
        private const string TimeTakenTicksKey = "NumberOfOperationsPerErrorsHandler_#_StopWatch_#_";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="instanceName"></param>
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
            _counter = new Lazy<PerformanceCounter>(() => new PerformanceCounter
            {
                CategoryName = CategoryName,
                CounterName = Name,
                InstanceName = GetInstanceName(newInstanceName),
                ReadOnly = false,
                InstanceLifetime = PerformanceCounterInstanceLifetime.Process,
                RawValue = 0
            });
        }

        protected override IEnumerable<CounterCreationData> DoGetCreationData()
        {
            yield return new CounterCreationData
            {
                CounterType = PerformanceCounterType.RateOfCountsPerSecond32,
                CounterName = Name,
                CounterHelp = "# of error operations / sec"
            };
        }
    }
}
