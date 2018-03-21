using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
#if NET452
using System.Runtime.Remoting.Messaging;
#endif


namespace PerfIt.Tests
{
    public class CorrelationTests
    {
        [Fact]
        public void CanSetIdIfNotThere()
        {
            Correlation.SetId(null);
            var id = Correlation.GetId();
            var idWithNoCheck = Correlation.GetId(setIfNotThere: false);
            Assert.Equal(id, idWithNoCheck);
        }

        [Fact]
        public void CanSetIdForAnyKey()
        {
            string key = Guid.NewGuid().ToString();
            var id = Correlation.GetId(key: key);
            var idWithNoCheck = Correlation.GetId(key: key, setIfNotThere: false);
            Assert.Equal(id, idWithNoCheck);
        }

        [Fact]
        public void HashCodeIsReliableForSamplingPurposes()
        {
            int total = 10*1000;
            double samplingRate = 0.3;
            int totalSampled = Enumerable.Range(0, total).Select(i => SimpleInstrumentor.ShouldInstrument(samplingRate, Guid.NewGuid().ToString()))
                .Count(x => x);
            Console.WriteLine(totalSampled);
            Assert.InRange(totalSampled, 2000, 4000);            
        }

        [Fact]
        public async Task CorrelationIdStaysTheSameAfterBloodyAsyncCalls()
        {
            var id = Correlation.GetId();
            await Task.Delay(100);
            var id2 = Correlation.GetId(setIfNotThere:false);

            Assert.Equal(id, id2);
        }

        [Fact]
        public async Task CorrelationIdStaysTheSameAfterBloodyAsyncCallsAndPublishEtw()
        {
            var id = Correlation.GetId();
            await Task.Delay(100);
            InstrumentationEventSource.Instance.WriteInstrumentationEvent("blah", "ff", 12,"gfg", id.ToString(), null);
            var id2 = Correlation.GetId(setIfNotThere: false);

            Assert.Equal(id, id2);
        }

        [Fact]
        public async Task CorrelationIdStaysTheSameAfterBloodyAsyncCallsAndPublishEtwAndCallingAsync()
        {
            var id = Correlation.GetId(setIfNotThere: true);
            id = Correlation.GetId(setIfNotThere: false);
            var inst = new SimpleInstrumentor(new InstrumentationInfo()
            {
                CategoryName = "cat",
                InstanceName = "ins",
                RaisePublishErrors = true
            });

            //InstrumentationEventSource.Instance.WriteInstrumentationEvent("blah", "ff", 12, "gfg", id.ToString());

            await inst.InstrumentAsync(() => Task.Delay(100), "not to worry");
            var id2 = Correlation.GetId(setIfNotThere: false);

            Assert.Equal(id, id2);
        }



        [Fact]
        public async Task CorrelationIdSetInAsyncGetsLostAfterFlowing()
        {
            await Task.Run(() =>
            {
                CallContext.LogicalSetData("crazy!!", "dotnet");
            });

            var afterId = CallContext.LogicalGetData("crazy!!");

            Assert.Null(afterId);
        }

        [Fact]
        public async Task ButIfSetInTheMainThreadItDoesFlow()
        {
            CallContext.LogicalSetData("crazy!!", "dotnet");

            await Task.Run(() =>
            {
                var inAsync = CallContext.LogicalGetData("crazy!!");
                Assert.NotNull(inAsync);
            });

            var afterId = CallContext.LogicalGetData("crazy!!");

            Assert.NotNull(afterId);
        }

    }
}
