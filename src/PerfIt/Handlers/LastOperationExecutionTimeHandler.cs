using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    /// <summary>
    /// Last Operation Execution Time Counter handler.
    /// </summary>
    public class LastOperationExecutionTimeHandler : CounterHandlerBase
    {
        private const string TimeTakenTicksKey = "LastOperationExecutionTimeHandler_#_StopWatch_#_";
        protected Lazy<PerformanceCounter> _counter;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="instanceName"></param>
        public LastOperationExecutionTimeHandler(string categoryName, string instanceName)
            : base(categoryName, instanceName)
        {
            BuildCounters();
        }
       
        public override string CounterType
        {
            get { return CounterTypes.LastOperationExecutionTime; }
        }

        protected override void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context)
        {
            context.Data.Add(TimeTakenTicksKey + InstanceName, Stopwatch.StartNew());
        }

        protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
        {
            var sw = (Stopwatch)context.Data[TimeTakenTicksKey + InstanceName];
            sw.Stop();
            _counter.Value.RawValue = sw.ElapsedMilliseconds;
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
                CounterType = PerformanceCounterType.NumberOfItems32,
                CounterName = Name,
                CounterHelp = "Time in ms to run last request"
            };
        }
    }
}
