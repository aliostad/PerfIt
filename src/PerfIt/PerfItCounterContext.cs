using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfIt
{
     internal class PerfItCounterContext
    {
        public string Name { get; set; }
        public ICounterHandler Handler { get; set; }

    }
}
