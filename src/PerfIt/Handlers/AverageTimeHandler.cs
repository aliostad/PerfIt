using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PerfIt
{
    /// <summary>
    /// Average Time Counter handler.
    /// </summary>
    public class AverageTimeHandler : CounterHandlerBase
    {
        /// <summary>
        /// "AverageTimeHandler_#_StopWatch_#_"
        /// </summary>
        private const string AverageTimeTakenTicksKey = "AverageTimeHandler_#_StopWatch_#_";

        private Lazy<PerformanceCounter> _counter;

        private Lazy<PerformanceCounter> _baseCounter;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="instanceName"></param>
        public AverageTimeHandler(string categoryName, string instanceName)
            : base(categoryName, instanceName)
        {
            BuildCounters();
        }

        public override string CounterType
        {
            get { return CounterTypes.AverageTimeTaken; }
        }

        protected override void OnRequestStarting(IDictionary<string, object> contextBag, PerfItContext context)
        {
            context.Data.Add(AverageTimeTakenTicksKey + InstanceName, Stopwatch.StartNew());
        }

        protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
        {
            var sw = (Stopwatch) context.Data[AverageTimeTakenTicksKey + InstanceName];
            sw.Stop();
            _counter.Value.IncrementBy(sw.ElapsedTicks);
            _baseCounter.Value.Increment();
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

            // TODO: TBD: consider IEnumerable<PerformanceCounter> pattern instead...
            _baseCounter = new Lazy<PerformanceCounter>(() => new PerformanceCounter
            {
                CategoryName = CategoryName,
                CounterName = GetBaseCounterName(),
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
                CounterType = PerformanceCounterType.AverageTimer32,
                CounterName = Name,
                CounterHelp = "Average seconds taken to execute"
            };

            yield return new CounterCreationData
            {
                CounterType = PerformanceCounterType.AverageBase,
                CounterName = GetBaseCounterName(),
                CounterHelp = "Average seconds taken to execute"
            };
        }

        private string GetBaseCounterName()
        {
            return "Total " + Name + " (base)";
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_counter != null && _counter.IsValueCreated)
            {
                _counter.Value.RemoveInstance();
                _counter.Value.Dispose();
            }

            if (_baseCounter != null && _baseCounter.IsValueCreated)
            {
                _baseCounter.Value.RemoveInstance();
                _baseCounter.Value.Dispose();
            }

        }
    }
}
