using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PerfIt.Tracers;
using Xunit;

namespace PerfIt.Tests
{
    public class TraceDataTests
    {
        [Fact]   
        public void SerialisesTimeCorrectly()
        {
            var d = new TraceData(new InstrumentationInfo(), 100, "cor", null);
            var s = JsonConvert.SerializeObject(d);
            var j = JObject.Parse(s);
            Assert.True(j.ContainsKey("time"));

        }
    }
}
