using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfIt
{
    public interface IPerfItContext
    {

        
         PerfItFilterAttribute Filter { get; set; }
         Dictionary<string, object> Data { get;  }
         ConcurrentBag<string> CountersToRun { get;  } 
    }
}
