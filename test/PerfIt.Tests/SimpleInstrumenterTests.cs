using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using Xunit;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Sinks;
using PerfIt.Tests.Stubs;

namespace PerfIt.Tests
{
    public class SimpleInstrumenterTests
    {
        private const string TestCategory = "PerfItTests";

        public SimpleInstrumenterTests()
        {
            PerfItRuntime.InstallStandardCounters(TestCategory);
        }

        [Fact]
        public void CanPublishAspect()
        {

            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = TestCategory
            });

            var listener = ConsoleLog.CreateListener();
            listener.EnableEvents(InstrumentationEventSource.Instance, EventLevel.LogAlways,
                Keywords.All);
            
            ins.Instrument(() => Thread.Sleep(100), "test...");
     
            listener.DisableEvents(InstrumentationEventSource.Instance);
            listener.Dispose();
        }

        [Fact]
        public void CanPublishAsyncAspect()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = TestCategory
            });

            ins.InstrumentAsync( () => Task.Delay(100), "test...").Wait();
        }

        [Fact]
        public void CanTurnOffPublishingCounters()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = false,
                PublishEvent = true,
                RaisePublishErrors = true
            });

            ins.InstrumentAsync(() => Task.Delay(100), "test...").Wait();
        }

        [Fact]
        public void DontRaiseErrorsDoesNotHideOriginalError()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            });

            var ex = Assert.Throws<AggregateException>(() => ins.InstrumentAsync(() => 
            {
                throw new NotImplementedException();
            }
                , "test...").Wait());

            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
        }

        [Fact]
        public void InstrumentorCreatesCorrIdIfNotExists()
        {
            var id = Correlation.GetId(setIfNotThere: false);
            if(id != null)
                Correlation.SetId(null);

            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = false,
                PublishEvent = true,
                RaisePublishErrors = true
            });

            ins.InstrumentAsync(() => Task.Delay(100), "test...").Wait();
            var idAfter = Correlation.GetId(setIfNotThere: false);
            Assert.NotNull(idAfter);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForSync()
        {
            int numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a,b,c,d, e) => numberOfTimesInstrumented++
            };

            double samplingRate = 0.01;
            Enumerable.Range(0,1000).ToList().ForEach(x => ins.Instrument( () => { }, samplingRate: samplingRate));

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForAsync()
        {
            int numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a, b, c, d, e) => numberOfTimesInstrumented++
            };

            double samplingRate = 0.01;
            Enumerable.Range(0, 1000).ToList().ForEach(x => ins.InstrumentAsync(async () => { }, samplingRate: samplingRate).Wait());

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForTwoStage()
        {
            int numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a, b, c, d, e) => numberOfTimesInstrumented++
            };

            double samplingRate = 0.01;
            Enumerable.Range(0, 1000).ToList().ForEach(x => ins.Finish(ins.Start(samplingRate)));

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }

        [Fact]
        public void InstrumentationShouldNotCallExcludedCounters()
        {
            if (!PerfItRuntime.HandlerFactories.ContainsKey("CustomCounterStub"))
                PerfItRuntime.HandlerFactories.Add("CustomCounterStub", (s, s1) => new CustomCounterStub(s, s1));

            CustomCounterStub.ClearCounters();

            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters,
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = TestCategory,
                PublishCounters = true,
            });

            ins.Instrument(() => { }, "test...");

            Assert.Equal(0, CustomCounterStub.RequestStartCount);
            Assert.Equal(0, CustomCounterStub.RequestEndCount);            
        }

        [Fact]
        public void InstrumentationShouldCallIncludedCounters()
        {
            if (!PerfItRuntime.HandlerFactories.ContainsKey("CustomCounterStub"))
                PerfItRuntime.HandlerFactories.Add("CustomCounterStub", (s, s1) => new CustomCounterStub(s, s1));

            CustomCounterStub.ClearCounters();

            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Counters = CounterTypes.StandardCounters.Union(new[] { "CustomCounterStub" }).ToArray(),
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = TestCategory,
                PublishCounters = true,
            });

            ins.Instrument(() => { }, "test...");

            Assert.Equal(1, CustomCounterStub.RequestStartCount);
            Assert.Equal(1, CustomCounterStub.RequestEndCount);
        }
    }
}
