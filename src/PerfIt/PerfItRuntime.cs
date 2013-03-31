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
            HandlerFactories = new Dictionary<string, Func<string, PerfItFilterAttribute, ICounterHandler>>();
        }

        /// <summary>
        /// Counter handler factories with counter type as the key.
        /// Factory's first param is applicationName and second is the filter
        /// Use it to register your own counters or replace built-in implementations
        /// </summary>
        public static Dictionary<string, Func<string, PerfItFilterAttribute, ICounterHandler>> HandlerFactories { get; private set; }
    
        /// <summary>
        /// Uninstalls performance counters in the current assembly using PerfItFilterAttribute.
        /// </summary>
        public static void Uninstall()
        {
            var perfItFilterAttributes = FindAllFilters();

            var cayegories = perfItFilterAttributes.ToList().Select(x => x.CategoryName).Distinct();
            cayegories.ToList().ForEach(PerformanceCounterCategory.Delete);
            
        }

        /// <summary>
        /// Installs performance counters in the current assembly using PerfItFilterAttribute.
        /// </summary>
        public static void Install()
        {
            var perfItFilterAttributes = FindAllFilters();
            var creationDataCollections = new Dictionary<string, CounterCreationDataCollection>();
            foreach (var filter in perfItFilterAttributes)
            {
                if (!creationDataCollections.ContainsKey(filter.CategoryName))
                    creationDataCollections.Add(filter.CategoryName, new CounterCreationDataCollection());

                foreach (var counterType in filter.Counters)
                {
                    if (!HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not defined: " + counterType);
                    using (var counterHandler = HandlerFactories[counterType]("Dummy, Not needed!", filter))
                    {
                        creationDataCollections[filter.CategoryName].AddRange(counterHandler.BuildCreationData(filter));
                    }
                }   
            }

            // now create them
            foreach (var categoryName in creationDataCollections.Keys)
            {
                PerformanceCounterCategory.Create(categoryName, "PerfIt category for " + categoryName,
                    PerformanceCounterCategoryType.MultiInstance, creationDataCollections[categoryName]);
            }
           
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
                            if (string.IsNullOrEmpty(attr.CategoryName))
                            {
                                attr.CategoryName = apiController.Assembly.GetName().Name;
                            }
                            attributes.Add(attr);
                        }
                        
                    }
                }
            }

            return attributes;
        }
    }
}
