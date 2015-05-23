using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PerfIt
{
    public interface IInspectDiscoverer
    {
        IEnumerable<IInstrumentationInfo> Discover(Assembly assembly);
    }
}
