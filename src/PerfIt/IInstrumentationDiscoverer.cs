using System.Collections.Generic;
using System.Reflection;

namespace PerfIt
{
    /// <summary>
    /// Discovers the <see cref="IInstrumentationInfo"/> for the <see cref="Assembly"/>.
    /// </summary>
    public interface IInstrumentationDiscoverer
    {
        /// <summary>
        /// Discovers the <see cref="IInstrumentationInfo"/> for the <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        IEnumerable<IInstrumentationInfo> Discover(Assembly assembly);
    }
}
