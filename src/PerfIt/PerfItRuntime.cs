using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PerfIt
{
    // TODO: TBD: re-factor me!
    /// <summary>
    /// PerfIt Runtime nerve center.
    /// </summary>
    public static class PerfItRuntime
    {
        static PerfItRuntime()
        {
            HandlerFactories = new Dictionary<string, Func<string, string, ICounterHandler>>
            {
                {
                    CounterTypes.TotalNoOfOperations,
                    (categoryName, instanceName) => new TotalCountHandler(categoryName, instanceName)
                },
                {
                    CounterTypes.AverageTimeTaken,
                    (categoryName, instanceName) => new AverageTimeHandler(categoryName, instanceName)
                },
                {
                    CounterTypes.LastOperationExecutionTime,
                    (categoryName, instanceName) => new LastOperationExecutionTimeHandler(categoryName, instanceName)
                },
                {
                    CounterTypes.NumberOfOperationsPerSecond,
                    (categoryName, instanceName) => new NumberOfOperationsPerSecondHandler(categoryName, instanceName)
                },
                {
                    CounterTypes.CurrentConcurrentOperationsCount,
                    (categoryName, instanceName) => new CurrentConcurrentCountHandler(categoryName, instanceName)
                },
                {
                    CounterTypes.NumberOfErrorsPerSecond,
                    (categoryName, instanceName) => new NumberOfErrorsPerSecondHandler(categoryName, instanceName)
                }
            };
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
        /// <param name="installerAssembly"></param>
        /// <param name="categoryName">if you have provided a categoryName for the installation, you must supply the same here</param>
        public static void Uninstall(Assembly installerAssembly, string categoryName = null)
        {

            if (string.IsNullOrEmpty(categoryName))
                categoryName = installerAssembly.GetName().Name;

            try
            {
                if (PerformanceCounterCategory.Exists(categoryName))
                    PerformanceCounterCategory.Delete(categoryName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Installs performance counters in the assembly
        /// </summary>
        /// <param name="installerAssembly"></param>
        /// <param name="discoverer">object that can discover aspects inside and assembly</param>
        /// <param name="categoryName">category name for the metrics. If not provided, it will use the assembly name</param>
        public static void Install(Assembly installerAssembly, IInstrumentationDiscoverer discoverer, string categoryName = null)
        {
            Uninstall(installerAssembly, discoverer, categoryName);

            if (string.IsNullOrEmpty(categoryName))
                categoryName = installerAssembly.GetName().Name;

            var instrumentationInfos = discoverer.Discover(installerAssembly).ToArray();

            if (instrumentationInfos.Length == 0)
            {
                throw new InvalidOperationException("There are no instrumentationInfos found by the discoverer!");
            }

            var counterCreationDataCollection = new CounterCreationDataCollection();
            Trace.TraceInformation("Number of filters: {0}", instrumentationInfos.Length);

            foreach (var group in instrumentationInfos.GroupBy(x => x.CategoryName))
            {
                foreach (var instrumentationInfo in group)
                {

                    Trace.TraceInformation("Setting up filters '{0}'", instrumentationInfo.Description);

                    foreach (var counterType in instrumentationInfo.Counters)
                    {
                        if (!HandlerFactories.ContainsKey(counterType))
                            throw new ArgumentException("Counter type not defined: " + counterType);

                        // if already exists in the set then ignore
                        if (counterCreationDataCollection.Cast<CounterCreationData>().Any(x => x.CounterName == counterType))
                        {
                            Trace.TraceInformation("Counter type '{0}' was duplicate", counterType);
                            continue;
                        }

                        using (var counterHandler = HandlerFactories[counterType](categoryName, instrumentationInfo.InstanceName))
                        {
                            counterCreationDataCollection.AddRange(counterHandler.BuildCreationData().ToArray());
                            Trace.TraceInformation("Added counter type '{0}'", counterType);
                        }
                    }
                }

                var catName = string.IsNullOrEmpty(group.Key) ? categoryName : group.Key;
                
                PerformanceCounterCategory.Create(catName, "PerfIt category for " + catName,
                     PerformanceCounterCategoryType.MultiInstance, counterCreationDataCollection);
            }

            Trace.TraceInformation("Built category '{0}' with {1} items", categoryName, counterCreationDataCollection.Count);
        }

        public static void Uninstall(Assembly installerAssembly, IInstrumentationDiscoverer discoverer, string categoryName = null)
        {

            if (string.IsNullOrEmpty(categoryName))
                categoryName = installerAssembly.GetName().Name;

            var perfItFilterAttributes = discoverer.Discover(installerAssembly).ToArray();
            Trace.TraceInformation("Number of filters: {0}", perfItFilterAttributes.Length);

            foreach (var group in perfItFilterAttributes.GroupBy(x => x.CategoryName))
            {
                var catName = string.IsNullOrEmpty(group.Key) ? categoryName : group.Key;
                Trace.TraceInformation("Deleted category '{0}'", catName);
                Uninstall(catName);
            }
        }

        /// <summary>
        ///  installs 4 standard counters for the category provided
        /// </summary>
        /// <param name="categoryName"></param>
        public static void InstallStandardCounters(string categoryName)
        {
            if (PerformanceCounterCategory.Exists(categoryName))
                return;

            var creationDatas = new CounterHandlerBase[]
            {
                new AverageTimeHandler(categoryName, string.Empty),
                new LastOperationExecutionTimeHandler(categoryName, string.Empty),
                new TotalCountHandler(categoryName, string.Empty),
                new NumberOfOperationsPerSecondHandler(categoryName, string.Empty) ,
                new CurrentConcurrentCountHandler(categoryName, string.Empty)
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

        public static string GetUniqueName(string instanceName, string counterType)
        {
            return string.Format("{0}.{1}", instanceName, counterType);
        }

        public static string GetCounterInstanceName(Type rootClass, string methodName)
        {
            return string.Format("{0}.{1}", rootClass.Name, methodName);
        }

        internal static bool IsFeatureEnabled(string featureName, string categoryName, bool defaultValue)
        {
            var value = ConfigurationManager.AppSettings[featureName];
            if (!string.IsNullOrEmpty(value))
            {
                return bool.Parse(value);
            }

            var categoryValue = ConfigurationManager.AppSettings[string.Format("{0}:{1}", featureName, categoryName)];
            if (!string.IsNullOrEmpty(categoryValue))
            {
                return bool.Parse(categoryValue);
            }

            return defaultValue;
        }

        public static bool IsPublishCounterEnabled(string catgeoryName, bool defaultValue)
        {
            return IsFeatureEnabled(Constants.PerfItPublishCounters, catgeoryName, defaultValue);
        }

        public static bool IsPublishErrorsEnabled(string catgeoryName, bool defaultValue)
        {
            return IsFeatureEnabled(Constants.PerfItPublishErrors, catgeoryName, defaultValue);
        }
        public static bool IsPublishEventsEnabled(string catgeoryName, bool defaultValue)
        {
            return IsFeatureEnabled(Constants.PerfItPublishEvent, catgeoryName, defaultValue);
        }
    }
}
