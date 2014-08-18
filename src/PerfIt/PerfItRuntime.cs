using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Http;
using PerfIt.Handlers;

namespace PerfIt
{
    public static class PerfItRuntime
    {
        static PerfItRuntime()
        {

            HandlerFactories = new Dictionary<string, Func<string, string, PerfItFilterAttribute, ICounterHandler>>();

            HandlerFactories.Add(CounterTypes.TotalNoOfOperations, 
                (categoryName, instanceName, filter) => new TotalCountHandler(categoryName, instanceName, filter));

            HandlerFactories.Add(CounterTypes.AverageTimeTaken,
                (categoryName, instanceName, filter) => new AverageTimeHandler(categoryName, instanceName, filter));

            HandlerFactories.Add(CounterTypes.LastOperationExecutionTime,
                (categoryName, instanceName, filter) => new LastOperationExecutionTimeHandler(categoryName, instanceName, filter));

            HandlerFactories.Add(CounterTypes.NumberOfOperationsPerSecond,
                (categoryName, instanceName, filter) => new NumberOfOperationsPerSecondHandler(categoryName, instanceName, filter));

            ThrowPublishingErrors = true;
        }

        /// <summary>
        /// Counter handler factories with counter type as the key.
        /// Factory's first param is applicationName and second is the filter
        /// Use it to register your own counters or replace built-in implementations
        /// </summary>
        public static Dictionary<string, Func<string, string, PerfItFilterAttribute, ICounterHandler>> HandlerFactories { get; private set; }
    
        /// <summary>
        /// Uninstalls performance counters in the current assembly using PerfItFilterAttribute.
        /// </summary>
        /// <param name="categoryName">if you have provided a categoryName for the installation, you must supply the same here</param>
        public static void Uninstall(string categoryName = null)
        {

            var frames = new StackTrace().GetFrames();
            var assembly = frames[1].GetMethod().ReflectedType.Assembly;
           
            try
            {
                if(PerformanceCounterCategory.Exists(categoryName))
                    PerformanceCounterCategory.Delete(categoryName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }       
                   
            
        }

        /// <summary>
        /// By default True. If false, errors encountered at publishing performance counters will be captured and 
        /// not thrown. They will be logged on the Trace.
        /// </summary>
        public static bool ThrowPublishingErrors { get; set; }

        /// <summary>
        /// Installs performance counters in the current assembly using PerfItFilterAttribute.
        /// </summary>
        /// <param name="categoryName">category name for the metrics. If not provided, it will use the assembly name</param>
        public static void Install(string categoryName = null)
        {
            Uninstall();

            var frames = new StackTrace().GetFrames();
            var assembly = frames[1].GetMethod().ReflectedType.Assembly;
            if (string.IsNullOrEmpty(categoryName))
                categoryName = assembly.GetName().Name;

            var perfItFilterAttributes = FindAllFilters(assembly);

            var counterCreationDataCollection = new CounterCreationDataCollection();

            foreach (var filter in perfItFilterAttributes)
            {
                foreach (var counterType in filter.Counters)
                {
                    if (!HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not defined: " + counterType);
                    using (var counterHandler = HandlerFactories[counterType](categoryName, filter.InstanceName, filter))
                    {
                        counterCreationDataCollection.AddRange(counterHandler.BuildCreationData());
                    }
                }   
            }

           
            PerformanceCounterCategory.Create(categoryName, "PerfIt category for " + categoryName,
                PerformanceCounterCategoryType.MultiInstance, counterCreationDataCollection);
            
           
        }

        /// <summary>
        /// Extracts all filters in the current assembly defined on ApiControllers
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<PerfItFilterAttribute> FindAllFilters(
            Assembly assembly)
        {
            

            var attributes = new List<PerfItFilterAttribute>();
               var apiControllers = assembly.GetExportedTypes()
                                           .Where(t => typeof(ApiController).IsAssignableFrom(t) &&
                                                        !t.IsAbstract);

            foreach (var apiController in apiControllers)
            {
                var controllerName = apiController.Name;
                var methodInfos = apiController.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                foreach (var methodInfo in methodInfos)
                {
                    var attr = (PerfItFilterAttribute)methodInfo.GetCustomAttributes(typeof(PerfItFilterAttribute), true).FirstOrDefault();
                    if (attr != null)
                    {
                        if (string.IsNullOrEmpty(attr.InstanceName)) // default name
                        {
                            var actionName = (ActionNameAttribute)
                                             methodInfo.GetCustomAttributes(typeof (ActionNameAttribute), true)
                                                       .FirstOrDefault();
                            if (actionName == null)
                            {
                                attr.InstanceName = controllerName + "." + methodInfo.Name;
                            }
                            else
                            {
                                attr.InstanceName = controllerName + "." + actionName.Name;
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
