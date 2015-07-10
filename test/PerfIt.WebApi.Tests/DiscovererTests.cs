using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PerfIt.WebApi.Tests
{
    public class DiscovererTests
    {

        [Fact]
        public void FindsTwoFilters()
        {
            var discoverer = new FilterDiscoverer();
            var infos = discoverer.Discover(Assembly.GetExecutingAssembly()).ToArray();
            Assert.Equal(2, infos.Length);
            Assert.True(infos.Any(x => x.CategoryName == "PerfItTests"));
            Assert.True(infos.Any(x => x.CategoryName == null));
        }
    }
}
