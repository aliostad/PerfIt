using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public class InstrumentationInfo : IInstrumentationInfo
    {
        public string InstanceName { get; set; }

        public string Description { get; set; }

        public string[] Counters { get; set; }

        public string CategoryName { get; set; }
    }
}
