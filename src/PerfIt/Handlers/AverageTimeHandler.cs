using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace PerfIt.Handlers
{
    public class AverageTimeHandler : CounterHandlerBase
    {

        private const string AverageTimeTakenTicksKey = "AverageTimeHandler_#_StopWatch_#_";
        private readonly Lazy<PerformanceCounter> _counter;
        private readonly Lazy<PerformanceCounter> _baseCounter;


        public AverageTimeHandler(string applicationName, PerfItFilterAttribute filter) : base(applicationName, filter)
        {

            _counter = new Lazy<PerformanceCounter>( () =>
                                                         {
                                                             var counter = new PerformanceCounter()
                                                                                          {
                                                                                              CategoryName = filter.CategoryName, 
                                                                                              CounterName = Name, 
                                                                                              InstanceName = applicationName, 
                                                                                              ReadOnly = false, 
                                                                                              InstanceLifetime = PerformanceCounterInstanceLifetime.Process
                                                                                          };
                                                             counter.RawValue = 0;
                                                             return counter;
                                                         }
                );


            _baseCounter = new Lazy<PerformanceCounter>(() =>
            {
                var counter = new PerformanceCounter()
                        {
                            CategoryName = filter.CategoryName,
                            CounterName = GetBaseCounterName(),
                            InstanceName = applicationName,
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
            get { return CounterTypes.AverageTimeTaken; }
        }

        protected override void OnRequestStarting(HttpRequestMessage request, PerfItContext context)
        {
            context.Data.Add(AverageTimeTakenTicksKey + Name, Stopwatch.StartNew());
        }

        protected override void OnRequestEnding(HttpResponseMessage response, PerfItContext context)
        {
            var sw = (Stopwatch) context.Data[AverageTimeTakenTicksKey + Name];
            sw.Stop();
            _counter.Value.IncrementBy(sw.ElapsedTicks);
            _baseCounter.Value.Increment();
        }

        protected override CounterCreationData[] DoGetCreationData()
        {
            var counterCreationDatas = new CounterCreationData[2];
            counterCreationDatas[0] = new CounterCreationData()
                                          {
                                              CounterType = PerformanceCounterType.AverageTimer32,
                                              CounterName = Name,
                                              CounterHelp = _filter.Description
                                          };
            counterCreationDatas[1] = new CounterCreationData()
                                          {
                                              CounterType = PerformanceCounterType.AverageBase,
                                              CounterName = GetBaseCounterName(),
                                              CounterHelp = _filter.Description
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
            if (_counter!=null && _counter.IsValueCreated)
            {
                _counter.Value.RemoveInstance();
                _counter.Value.Dispose();                
            }
            if (_baseCounter!=null && _baseCounter.IsValueCreated)
            {
                _baseCounter.Value.RemoveInstance();
                _baseCounter.Value.Dispose();                
            }

        }
    }
}
