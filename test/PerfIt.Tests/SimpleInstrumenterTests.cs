using System;
using System.Collections.Generic;
using System.Diagnostics;
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
#if NET452
    public class PerfCounterIgnoreFactAttribute : FactAttribute
    {
        public PerfCounterIgnoreFactAttribute(string categoryName)
        {            
            if (!PerformanceCounterCategory.GetCategories().Any(x => x.CategoryName == categoryName))
            {
                Skip = $"Please install {categoryName} Performance Counter category to run.";
            }
        }
    }
#endif


    public class SimpleInstrumenterTests
    {
        private const string TestCategory = "PerfItTests";

        private class ActionTracer : ITwoStageTracer
        {
            public ActionTracer(Action action)
            {
                TheAction = action;
            }

            public Action TheAction { get; }

            public void Dispose()
            {

            }

            public void Finish(object token, 
                long timeTakenMilli, 
                string correlationId = null, 
                InstrumentationContext extraContext = null)
            {
                TheAction();
            }

            public object Start(IInstrumentationInfo info)
            {
                return info;
            }
        }

        [Fact]
        public void CanPublishAspect()
        {

            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = TestCategory
            });
            
            ins.Instrument(() => Thread.Sleep(100));
     
        }

        [Fact]
        public void CanPublishAsyncAspect()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Testinstance",
                CategoryName = TestCategory
            });

            ins.InstrumentAsync( () => Task.Delay(100)).Wait();
        }


 #if NET452
        [PerfCounterIgnoreFactAttribute("test")]
        public void WorksWithEnabledCounters()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Testinstance",
                CategoryName = TestCategory,
                PublishCounters = true,
                RaisePublishErrors = true
            });
            for (int i = 0; i < 100; i++)
            {
                ins.InstrumentAsync(() => Task.Delay(100)).Wait();
            }
        }
#endif
        [Fact]
        public void DontRaiseErrorsDoesNotHideOriginalError()
        {
            var ins = new SimpleInstrumentor(new InstrumentationInfo()
            {
                Description = "test",
                InstanceName = "Test instance",
                CategoryName = "DOESNOTEXISTDONTLOOKFORIT",
                RaisePublishErrors = false
            });

            var ex = Assert.Throws<AggregateException>(() => ins.InstrumentAsync(() => 
            {
                throw new NotImplementedException();
            }).Wait());

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
                RaisePublishErrors = true
            });

            await ins.InstrumentAsync(() => Task.Delay(100));
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
                RaisePublishErrors = false
            });

            ins.Tracers.Add("a", new ActionTracer(() => numberOfTimesInstrumented++));

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
                RaisePublishErrors = false
            });

            ins.Tracers.Add("a", new ActionTracer(() => numberOfTimesInstrumented++));

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
                RaisePublishErrors = false
            });

            ins.Tracers.Add("a", new ActionTracer(() => numberOfTimesInstrumented++));

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
