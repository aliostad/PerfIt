using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Xunit;

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
            var ins = new SimpleInstrumentor(new InstrumentationInfo
            {
                Counters = CounterTypes.StandardCounters.ToArray(),
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
            var ins = new SimpleInstrumentor(new InstrumentationInfo
            {
                Counters = CounterTypes.StandardCounters.ToArray(),
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = TestCategory
            });

            ins.InstrumentAsync(() => Task.Delay(100), "test...").Wait();
        }

        [Fact]
        public void CanTurnOffPublishingCounters()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo
            {
                Counters = CounterTypes.StandardCounters.ToArray(),
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
            var ins = new SimpleInstrumentor(new InstrumentationInfo
            {
                Counters = CounterTypes.StandardCounters.ToArray(),
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
            }, "test...").Wait());

            Assert.IsType<NotImplementedException>(ex.InnerExceptions[0]);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForSync()
        {
            var numberOfTimesInstrumented = 0;

            var ins = new SimpleInstrumentor(new InstrumentationInfo
            {
                Counters = CounterTypes.StandardCounters.ToArray(),
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a, b, c, d) => numberOfTimesInstrumented++
            };

            const double samplingRate = 0.01d;
            Enumerable.Range(0, 1000).ToList().ForEach(x => ins.Instrument(() => { }, samplingRate: samplingRate));

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForAsync()
        {
            var numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo
            {
                Counters = CounterTypes.StandardCounters.ToArray(),
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a, b, c, d) => numberOfTimesInstrumented++
            };

            const double samplingRate = 0.01d;

            Enumerable.Range(0, 1000).ToList()
                .ForEach(x => ins.InstrumentAsync(async () => { }, samplingRate: samplingRate).Wait());

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForTwoStage()
        {
            var numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo
            {
                Counters = CounterTypes.StandardCounters.ToArray(),
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a, b, c, d) => numberOfTimesInstrumented++
            };

            const double samplingRate = 0.01d;

            Enumerable.Range(0, 1000).ToList().ForEach(x => ins.Finish(ins.Start(samplingRate)));

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }
    }
}
