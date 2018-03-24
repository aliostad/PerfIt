using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
#if NETCOREAPP2_0
using Microsoft.Extensions.Configuration;
#endif

namespace PerfIt.Tests
{
    public class PerfItRuntimeTests
    {
#if NETCOREAPP2_0
        public PerfItRuntimeTests()
        {
            var dic = new Dictionary<string, string>
            {
                { "perfit:publishCounters", "true" },
                { "perfit:publishErrors:a", "false" },
                { "perfit:publishErrors:b", "true" },
                { "perfit#publishErrors", "true" },
                { "perfit:publishEvent:c", "false" },
                { "perfit:samplingRate:c", "0.1" },
                { "perfit:samplingRate", "0.2" },
                { "perfit#publishEvent#cc", "false" },
                { "perfit#samplingRate#cc", "0.3" }
            };
            
            var cfg = new ConfigurationBuilder().AddInMemoryCollection(dic).Build();
            PerfItRuntime.ConfigurationProvider = (s) => cfg[s];
        }
#endif
        [Fact]
        public void SettingGlobalPublishCountersInConfigWorks()
        {
            Assert.Equal(true, PerfItRuntime.IsPublishCounterEnabled("any", false));
            Assert.Equal(true, PerfItRuntime.IsPublishCounterEnabled("other", false));
        }

        [Fact]
        public void SettingCategoryPublishErrorsInConfigWorks()
        {
            Assert.Equal(true, PerfItRuntime.IsPublishErrorsEnabled("any", false));
        }

        [Fact]
        public void SettingCategoryPublishErrorsInConfigForCategoryOverridesGlobal()
        {
            Assert.Equal(false, PerfItRuntime.IsPublishErrorsEnabled("a", true));
            Assert.Equal(true, PerfItRuntime.IsPublishErrorsEnabled("b", false));
        }

        [Fact]
        public void SettingCategoryPublishEventsInConfigWorks()
        {
            Assert.Equal(false, PerfItRuntime.IsPublishEventsEnabled("c", true));
        }

        [Fact]
        public void SettingSamplingRateInConfigWorks()
        {
            Assert.Equal(0.2, PerfItRuntime.GetSamplingRate("any", 2));
        }

        [Fact]
        public void SettingSamplingRateInConfigForCategoryOverridesGlobal()
        {
            Assert.Equal(0.1, PerfItRuntime.GetSamplingRate("c", 2));
        }

        [Fact]
        public void SettingSamplingRateInConfigForCategoryOverridesGlobalIfSpecifiedWithHash()
        {
            Assert.Equal(0.3, PerfItRuntime.GetSamplingRate("cc", 2));
        }

    }
}
