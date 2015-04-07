using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http;
using PerfIt.Handlers;
using System.Configuration;
using System.Collections.Concurrent;

namespace PerfIt
{
    public static class PerfItRuntime
    {
       

        static PerfItRuntime()
        {
            PublishCounters = true;
            RaisePublishErrors = true;
            SetErrorPolicy();
            SetPublish();

            MonitoredCountersContexts = new ConcurrentDictionary<string, PerfItCounterContext>();

            HandlerFactories = new Dictionary<string, Func<string, string, ICounterHandler>>();

            HandlerFactories.Add(CounterTypes.TotalNoOfOperations, 
                (categoryName, instanceName) => new TotalCountHandler(categoryName, instanceName));

            HandlerFactories.Add(CounterTypes.AverageTimeTaken,
                (categoryName, instanceName) => new AverageTimeHandler(categoryName, instanceName));

            HandlerFactories.Add(CounterTypes.LastOperationExecutionTime,
                (categoryName, instanceName) => new LastOperationExecutionTimeHandler(categoryName, instanceName));

            HandlerFactories.Add(CounterTypes.NumberOfOperationsPerSecond,
                (categoryName, instanceName) => new NumberOfOperationsPerSecondHandler(categoryName, instanceName));

            HandlerFactories.Add(CounterTypes.CurrentConcurrentOperationsCount,
     (categoryName, instanceName) => new CurrentConcurrentCountHandler(categoryName, instanceName));


        }

        public static bool PublishCounters { get; private set; }

        public static bool RaisePublishErrors { get; private set; }


        internal static ConcurrentDictionary<string, PerfItCounterContext> MonitoredCountersContexts { get; private set; }

        private static void  SetPublish()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishCounters] ?? PublishCounters.ToString();
            PublishCounters = Convert.ToBoolean(value);
        }

        private static void SetErrorPolicy()
        {
            var value = ConfigurationManager.AppSettings[Constants.PerfItPublishErrors] ?? RaisePublishErrors.ToString();
            RaisePublishErrors = Convert.ToBoolean(value);
        }

        /// <summary>
        /// Counter handler factories with counter type as the key.
        /// Factory's first param is applicationName and second is the filter
        /// Use it to register your own counters or replace built-in implementations
        /// </summary>
        public static Dictionary<string, Func<string, string, ICounterHandler>> HandlerFactories { get; private set; }
    
        /// <summary>
        /// Uninstalls performance counters in the current assembly using PerfItFilterAttribute.
        /// </summary>
        /// <param name="categoryName">if you have provided a categoryName for the installation, you must supply the same here</param>
        public static void Uninstall(Assembly installerAssembly, string categoryName = null)
        {

            if (string.IsNullOrEmpty(categoryName))
                categoryName = installerAssembly.GetName().Name;

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
        /// Installs performance counters in the current assembly using PerfItFilterAttribute.
        /// </summary>
        /// 
        /// <param name="categoryName">category name for the metrics. If not provided, it will use the assembly name</param>
        public static void Install(Assembly installerAssembly, string categoryName = null)
        {
            Uninstall(installerAssembly, categoryName);

            if (string.IsNullOrEmpty(categoryName))
                categoryName = installerAssembly.GetName().Name;
            
            var perfItFilterAttributes = FindAllPerfItAttributes(installerAssembly).ToArray();

            var counterCreationDataCollection = new CounterCreationDataCollection();

            Trace.TraceInformation("Number of filters: {0}", perfItFilterAttributes.Length);

            foreach (var filter in perfItFilterAttributes)
            {

                Trace.TraceInformation("Setting up filters '{0}'", filter.Description);

                foreach (var counterType in filter.Counters)
                {
                    if (!HandlerFactories.ContainsKey(counterType))
                        throw new ArgumentException("Counter type not defined: " + counterType);

                    // if already exists in the set then ignore
                    if (counterCreationDataCollection.Cast<CounterCreationData>().Any(x => x.CounterName == counterType))
                    {
                        Trace.TraceInformation("Counter type '{0}' was duplicate", counterType);
                        continue;                        
                    }

                    using (var counterHandler = HandlerFactories[counterType](categoryName, filter.InstanceName))
                    {
                        counterCreationDataCollection.AddRange(counterHandler.BuildCreationData());
                        Trace.TraceInformation("Added counter type '{0}'", counterType);
                    }
                }   
            }
           

            PerformanceCounterCategory.Create(categoryName, "PerfIt category for " + categoryName,
                PerformanceCounterCategoryType.MultiInstance, counterCreationDataCollection);

            Trace.TraceInformation("Built category '{0}' with {1} items", categoryName, counterCreationDataCollection.Count);
            
           
        }

        /// <summary>
        ///  installs 4 standard counters for the category provided
        /// </summary>
        /// <param name="categoryName"></param>

        public static void InstallStandardCounters(string categoryName)
        {
            var creationDatas = new CounterHandlerBase[]
            {
                new AverageTimeHandler(categoryName, string.Empty),
                new LastOperationExecutionTimeHandler(categoryName, string.Empty),
                new TotalCountHandler(categoryName, string.Empty),
                new NumberOfOperationsPerSecondHandler(categoryName, string.Empty) 
            }.SelectMany(x => x.BuildCreationData());

            var counterCreationDataCollection = new CounterCreationDataCollection();
            counterCreationDataCollection.AddRange(creationDatas.ToArray());
            PerformanceCounterCategory.Create(categoryName, "PerfIt category for " + categoryName,
                PerformanceCounterCategoryType.MultiInstance, counterCreationDataCollection);

        }

        /// <summary>
        ///  Uninstalls the category provided
        /// </summary>
        /// <param name="categoryName"></param>
        public static void Uninstall(string categoryName)
        {

            if (PerformanceCounterCategory.Exists(categoryName))
                PerformanceCounterCategory.Delete(categoryName);           
        }

        internal static string GetUniqueName(string instanceName, string counterType)
        {
            return string.Format("{0}.{1}", instanceName, counterType);
        }

        internal static string GetCounterInstanceName(Type controllerType, string actionName)
        {
            return string.Format("{0}_{1}", controllerType.FullName, actionName);
        }

       
        /// <summary>
        /// Extracts all PerfIt attributes in the current assembly defined on Service Classes
        /// </summary>
        /// <returns></returns>
        internal static IEnumerable<IPerfItAttribute> FindAllPerfItAttributes(
            Assembly assembly)
        {

            
            var attributes = new List<IPerfItAttribute>();
            var servicesToMonitor = assembly.GetExportedTypes();
               
               Trace.TraceInformation("Found '{0}' classes", servicesToMonitor.Length);

               foreach (var service in servicesToMonitor)
            {
                attributes.AddRange((List<IPerfItAttribute>)FindAllPerfItAttributes((Type)service));
            }

            return attributes;
        }

        internal static IEnumerable<IPerfItAttribute> FindAllPerfItAttributes(
            Type monitoredClass)
        {


            var attributes = new List<IPerfItAttribute>();

            var methodInfos = monitoredClass.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(t => t.GetCustomAttributes(typeof(IPerfItAttribute), true).Length > 0 ).ToArray();

            
            foreach (var methodInfo in methodInfos)
            {
                var attr = FindPerfItAttribute(methodInfo);
                if (attr != null)
                {
                    attributes.Add(attr);
                }
            }

            return attributes;
        }

        public static IPerfItAttribute FindPerfItAttribute(MethodInfo methodInfo)
        {
            var attr = (IPerfItAttribute)methodInfo.GetCustomAttributes(typeof(IPerfItAttribute), true).FirstOrDefault();
            if (attr != null)
            {
                if (string.IsNullOrEmpty(attr.InstanceName)) // default name
                {
                    var actionNameAttr = (ActionNameAttribute)
                                        methodInfo.GetCustomAttributes(typeof(ActionNameAttribute), true)
                                                .FirstOrDefault();

                    string actionName = actionNameAttr == null ? methodInfo.Name : actionNameAttr.Name;
                    attr.InstanceName = GetCounterInstanceName(methodInfo.DeclaringType, actionName);
                }

                Trace.TraceInformation("Added '{0}' to the list", attr.Counters);
                return attr;


            }
            else
            {
                return null;
            }
        }

        
    }


}
