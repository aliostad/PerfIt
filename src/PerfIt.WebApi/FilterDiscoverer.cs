using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace PerfIt.WebApi
{
    public class FilterDiscoverer : IInstrumentationDiscoverer
    {

        internal static IEnumerable<IInstrumentationInfo> FindAllFilters(
            Assembly assembly)
        {

            var attributes = new List<PerfItFilterAttribute>();
            var apiControllers = assembly.GetExportedTypes()
                .Where(t => typeof (ApiController).IsAssignableFrom(t) &&
                            !t.IsAbstract).ToArray();

            Trace.TraceInformation("Found '{0}' controllers", apiControllers.Length);

            foreach (var apiController in apiControllers)
            {
                var methodInfos = apiController.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var methodInfo in methodInfos)
                {
                    var attr =
                        (PerfItFilterAttribute)
                            methodInfo.GetCustomAttributes(typeof (PerfItFilterAttribute), true).FirstOrDefault();
                    if (attr != null)
                    {
                        if (string.IsNullOrEmpty(attr.InstanceName)) // !!! NOTE: default name - this is just a hacky fallback to get install working runtime instance name could be different
                        {
                            var actionNameAttr = (ActionNameAttribute)
                                methodInfo.GetCustomAttributes(typeof (ActionNameAttribute), true)
                                    .FirstOrDefault();

                            string actionName = actionNameAttr == null ? methodInfo.Name : actionNameAttr.Name;
                            attr.InstanceName = PerfItRuntime.GetCounterInstanceName(apiController, actionName);
                        }

                        attributes.Add(attr);
                        Trace.TraceInformation("Added '{0}' to the list", attr.Counters);

                    }
                }
            }

            return attributes;
        }

        public IEnumerable<IInstrumentationInfo> Discover(Assembly assembly)
        {
            return FindAllFilters(assembly);
        }
    }


}