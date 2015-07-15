using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace PerfIt.Castle.Interception
{
    public class AttributeDiscoverer : IInstrumentationDiscoverer
    {

        internal static IInstrumentationInfo FindAttribute(
            MethodInfo methodInfo)
        {
           var attr = (IInstrumentationInfo)methodInfo.GetCustomAttributes(typeof(PerfItAttribute), true).FirstOrDefault();
           return attr;
        }

        internal static IEnumerable<IInstrumentationInfo> FindAllAttributes(
            Assembly assembly)
        {

            
            var attributes = new List<PerfItAttribute>();
            var components = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract).ToArray();

            Trace.TraceInformation("Found '{0}' components", components.Length);

            foreach (var component in components)
            {
                var methodInfos = component.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var methodInfo in methodInfos)
                {
                    var attr = (PerfItAttribute)FindAttribute(methodInfo);
                        
                    if (attr != null)
                    {
                        if (string.IsNullOrEmpty(attr.InstanceName)) // !!! NOTE: default name - this is just a hacky fallback to get install working runtime instance name could be different
                        {
                           
                            string actionName = methodInfo.Name ;
                            attr.InstanceName = PerfItRuntime.GetCounterInstanceName(component, actionName);
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
            return FindAllAttributes(assembly);
        }

       
    }


}