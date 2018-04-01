using PerfIt.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace PerfIt.Samples.WebApi
{
    public class TestController : ApiController
    {
        [PerfItFilter("server-test", PublishCounters = false)]
        public string Get()
        {
            Thread.Sleep(100);
            return Guid.NewGuid().ToString();
        }
    }
}
