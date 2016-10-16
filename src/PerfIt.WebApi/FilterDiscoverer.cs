using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web.Http;

namespace PerfIt.WebApi
{
    // TODO: TBD: this looks an awful lot like the same cross-cutting concern... but for subtle API differences, that could be captured by strategic Attribute and interface declarations...
    public class FilterDiscoverer : IInstrumentationDiscoverer
    {
        internal static IEnumerable<IInstrumentationInfo> FindAllInfos(Assembly assembly)
        {
            var apiControllers = assembly.GetExportedTypes()
                .Where(t => typeof(ApiController).IsAssignableFrom(t)
                            && !t.IsAbstract).ToArray();

            Trace.TraceInformation("Found '{0}' controllers", apiControllers.Length);

            foreach (var apiController in apiControllers)
            {
                var methodInfos = apiController.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var methodInfo in methodInfos)
                {
                    var attr = methodInfo.GetCustomAttributes<PerfItFilterAttribute>(true).FirstOrDefault();

                    if (attr == null) continue;

                    // !!! NOTE: default name - this is just a hacky fallback to get install working runtime instance name could be different
                    if (string.IsNullOrEmpty(attr.InstanceName))
                    {
                        var actionNameAttr = methodInfo.GetCustomAttributes<ActionNameAttribute>(true).FirstOrDefault();
                        var actionName = actionNameAttr == null ? methodInfo.Name : actionNameAttr.Name;
                        attr.InstanceName = PerfItRuntime.GetCounterInstanceName(apiController, actionName);
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
