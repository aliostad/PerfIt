using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PerfIt.Castle.Interception
{
    public class AttributeDiscoverer : IInstrumentationDiscoverer
    {
        // TODO: TBD: this could potentially be a cross cutting concern, without needing to repeat the GetCustomAttributes in different places
        internal static IEnumerable<IInstrumentationInfo> FindAllInfos(Assembly assembly)
        {
            var components = assembly.GetExportedTypes().Where(t => !t.IsAbstract).ToArray();

            Trace.TraceInformation("Found '{0}' components", components.Length);

            foreach (var component in components)
            {
                // TODO: TBD: only public methods? also properties? protected? internal? private? static?
                var methodInfos = component.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var methodInfo in methodInfos)
                {
                    var attr = methodInfo.GetCustomAttributes<PerfItAttribute>(true).FirstOrDefault();

                    if (attr == null) continue;

                    // !!! NOTE: default name - this is just a hacky fallback to get install working runtime instance name could be different
                    if (string.IsNullOrEmpty(attr.InstanceName))
                    {
                        var actionName = methodInfo.Name;
                        attr.InstanceName = PerfItRuntime.GetCounterInstanceName(component, actionName);
                    }

                    Trace.TraceInformation("Added '{0}' to the list", string.Join(", ", attr.Counters));

                    yield return attr.Info;
                }
            }
        }

        public IEnumerable<IInstrumentationInfo> Discover(Assembly assembly)
        {
            return FindAllInfos(assembly);
        }
    }
}
