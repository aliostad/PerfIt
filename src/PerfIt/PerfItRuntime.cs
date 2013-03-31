using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Http;

namespace PerfIt
{
    public static class PerfItRuntime
    {
        static PerfItRuntime()
        {
            HandlerFactories = new Dictionary<string, Func<string, string, ICounterHandler>>();
        }

        /// <summary>
        /// Counter handler factories with counter type as the key.
        /// Factory's first param is applicationName and second is counterName
        /// </summary>
        public static Dictionary<string, Func<string, string, ICounterHandler>> HandlerFactories { get; private set; }  


        /// <summary>
        /// Installs performance counters in the current assembly using PerfItFilterAttribute.
        /// Uses assembly name as the application name which becomes counters category.
        /// </summary>
        public static void Install()
        {
            Install(GetDefaultApplicationName());
        }

        internal static string GetDefaultApplicationName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        /// <summary>
        /// Uninstalls performance counters in the current assembly using PerfItFilterAttribute.
        /// Uses assembly name as the application name which becomes counters category.
        /// </summary>
        public static void Uninstall()
        {
            Uninstall(GetDefaultApplicationName());
        }

        /// <summary>
        /// Uninstalls performance counters in the current assembly using PerfItFilterAttribute.
        /// Uses name provided as the application name which becomes counters category.
        /// </summary>
        public static void Uninstall(string applicationName)
        {
            PerformanceCounterCategory.Delete(applicationName);
        }

        /// <summary>
        /// Installs performance counters in the current assembly using PerfItFilterAttribute.
        /// Uses name provided as the application name which becomes counters category.
        /// </summary>
        public static void Install(string applicationName)
        {
            var perfItFilterAttributes = FindAllFilters();
            var creationDataCollection = new CounterCreationDataCollection();
            foreach (var filter in perfItFilterAttributes)
            {
                foreach (var counterType in filter.Counters)
                {
                    if (!HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not defined: " + counterType);
                    using (var counterHandler = HandlerFactories[counterType](applicationName, filter.Name))
                    {
                        creationDataCollection.AddRange(counterHandler.BuildCreationData(filter));
                    }
                }
                
            }

            PerformanceCounterCategory.Create(applicationName, "PerfIt category for " + applicationName, 
                PerformanceCounterCategoryType.MultiInstance,
                                                 creationDataCollection);
        }

        /// <summary>
        /// Extracts all filters in the current assembly defined on ApiControllers
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<PerfItFilterAttribute> FindAllFilters()
        {
            var attributes = new List<PerfItFilterAttribute>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var apiControllers = assemblies.SelectMany(x => x.GetExportedTypes())
                                           .Where(t => typeof(ApiController).IsAssignableFrom(t));

            foreach (var apiController in apiControllers)
            {
                var controllerName = apiController.Name;
                var methodInfos = apiController.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var methodInfo in methodInfos)
                {
                    var attr = (PerfItFilterAttribute)methodInfo.GetCustomAttributes(typeof(PerfItFilterAttribute), true).FirstOrDefault();
                    if (attr != null)
                    {
                        if (string.IsNullOrEmpty(attr.Name)) // default name
                        {
                            var actionName = (ActionNameAttribute)
                                             methodInfo.GetCustomAttributes(typeof (ActionNameAttribute), true)
                                                       .FirstOrDefault();
                            if (actionName == null)
                            {
                                attr.Name = controllerName + "." + methodInfo.Name;
                            }
                            else
                            {
                                attr.Name = controllerName + "." + actionName.Name;
                            }
                        }
                        
                        attributes.Add(attr);
                    }
                }
            }

            return attributes;
        }
    }
}
