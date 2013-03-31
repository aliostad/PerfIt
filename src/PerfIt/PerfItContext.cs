using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfIt
{
    public class PerfItContext
    {

        public PerfItContext()
        {
            Data = new Dictionary<string, object>();    
        }

        public PerfItFilterAttribute Filter { get; set; }
        public Dictionary<string, object> Data { get; private set; } 
    }
}
