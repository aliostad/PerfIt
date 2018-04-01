using PerfIt.CoreMvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PerfIt.Samples.CoreMvc
{
    public class TestController
    {
        [PerfItFilter("server-test")]
        public string Get()
        {
            Thread.Sleep(100);
            return Guid.NewGuid().ToString();
        }
    }
}
