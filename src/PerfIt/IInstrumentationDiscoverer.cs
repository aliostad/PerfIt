#if NET452
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PerfIt
{
    public interface IInstrumentationDiscoverer
    {
        IEnumerable<IInstrumentationInfo> Discover(Assembly assembly);
    }
}
#endif