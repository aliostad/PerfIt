using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    /// <summary>
    /// Total Count Counter handler.
    /// </summary>
    public class TotalCountHandler : CounterHandlerBase
    {
        private Lazy<PerformanceCounter> _counter;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="instanceName"></param>
        public TotalCountHandler(string categoryName, string instanceName)
            : base(categoryName, instanceName)
        {           
            BuildCounters();
        }

        public override string CounterType
        {
            get { return CounterTypes.TotalNoOfOperations; }
        }

        protected override void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context)
        {
            // nothing 
        }

        protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
        {
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
                CounterName = Name,
                CounterType = PerformanceCounterType.NumberOfItems32,
                CounterHelp = "Total # of operations"
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
