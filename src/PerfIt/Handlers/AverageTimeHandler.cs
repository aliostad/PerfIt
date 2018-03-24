#if NET452
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PerfIt.Handlers
{
    public class AverageTimeHandler : CounterHandlerBase
    {

        private const string AverageTimeTakenTicksKey = "AverageTimeHandler_#_StopWatch_#_";
        private Lazy<PerformanceCounter> _counter;
        private Lazy<PerformanceCounter> _baseCounter;


        public AverageTimeHandler(
            string categoryName,
            string instanceName)
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
            context.Data.Add(AverageTimeTakenTicksKey + _instanceName, Stopwatch.StartNew());
        }

        protected override void OnRequestEnding(IDictionary<string, object> contextBag, PerfItContext context)
        {
            var sw = (Stopwatch)context.Data[AverageTimeTakenTicksKey + _instanceName];
            sw.Stop();
            _counter.Value.IncrementBy(sw.ElapsedTicks);
            _baseCounter.Value.Increment();
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

            _baseCounter = new Lazy<PerformanceCounter>(() =>
            {
                var counter = new PerformanceCounter()
                {
                    CategoryName = _categoryName,
                    CounterName = GetBaseCounterName(),
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
            var counterCreationDatas = new CounterCreationData[2];
            counterCreationDatas[0] = new CounterCreationData()
            {
                CounterType = PerformanceCounterType.AverageTimer32,
                CounterName = Name,
                CounterHelp = "Average seconds taken to execute"
            };
            counterCreationDatas[1] = new CounterCreationData()
            {
                CounterType = PerformanceCounterType.AverageBase,
                CounterName = GetBaseCounterName(),
                CounterHelp = "Average seconds taken to execute"
            };
            return counterCreationDatas;
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
#endif