using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{

    // PerfitHandlerContext
    public class PerfitHandlerContext
    {
        public string Name { get; set; }
        public ICounterHandler Handler { get; set; }

    }
}
