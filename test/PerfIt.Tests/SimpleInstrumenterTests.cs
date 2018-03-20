using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
#if NET452
using System.Runtime.Remoting.Messaging;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PerfIt.Tests
{
    public class SimpleInstrumenterTests
    {
        private const string TestCategory = "PerfItTests";

        [Fact]
        public void CanPublishAspect()
        {

            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = TestCategory
            });
            
            ins.Instrument(() => Thread.Sleep(100), "test...");
     
        }

        [Fact]
        public void CanPublishAsyncAspect()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
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

        // http://blog.stephencleary.com/2013/04/implicit-async-context-asynclocal.html Thanks to Kristian Hellang
        [Fact(Skip = "Bear in mind!! This is the side effect!!! If the cor-id gets set in an async, it does not get flowed??!!")]
        public async Task InstrumentorCreatesCorrIdIfNotExists()
        { 
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = false,
                PublishEvent = true,
                RaisePublishErrors = true
            });

            await ins.InstrumentAsync(() => Task.Delay(100), "test...");
            var idAfter = Correlation.GetId(setIfNotThere: false);
            Assert.NotNull(idAfter);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForSync()
        {
            int numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = true,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a,b,c,d, e, f) => numberOfTimesInstrumented++
            };

            double samplingRate = 0.01;
            Enumerable.Range(0, 1000).ToList().ForEach(x =>
            {
                Correlation.SetId(Guid.NewGuid().ToString());
                ins.Instrument(() => { }, samplingRate: samplingRate);
            }); 

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForAsync()
        {
            int numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = false,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a, b, c, d, e, f) => numberOfTimesInstrumented++
            };

            double samplingRate = 0.01;
            Enumerable.Range(0, 1000).ToList().ForEach(x =>
            {
                Correlation.SetId(Guid.NewGuid().ToString());
                ins.InstrumentAsync(async () => { }, samplingRate: samplingRate).Wait();
            });

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }

        [Fact]
        public void InstrumentationSamplingRateLimitsForTwoStage()
        {
            int numberOfTimesInstrumented = 0;
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                PublishCounters = false,
                PublishEvent = true,
                RaisePublishErrors = false
            })
            {
                PublishInstrumentationCallback = (a, b, c, d, e, f) => numberOfTimesInstrumented++
            };

            double samplingRate = 0.01;
            Enumerable.Range(0, 1000).ToList().ForEach(x =>
            {
                Correlation.SetId(Guid.NewGuid().ToString());
                ins.Finish(ins.Start(samplingRate));
            });

            Assert.InRange(numberOfTimesInstrumented, 1, 100);
        }
    }
}
