using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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

    }
}
