#if NET452
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PerfIt.Handlers;

namespace PerfIt
{
    public static class CounterInstaller
    {

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
        public static void Install(Assembly installerAssembly,
            IInstrumentationDiscoverer discoverer,
            string categoryName = null)
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

                    if (instrumentationInfo.Counters == null || instrumentationInfo.Counters.Length == 0)
                        instrumentationInfo.Counters = CounterTypes.StandardCounters;

                    foreach (var counterType in instrumentationInfo.Counters)
                    {
                        if (!PerfItRuntime.HandlerFactories.ContainsKey(counterType))
                            throw new ArgumentException("Counter type not defined: " + counterType);

                        // if already exists in the set then ignore
                        if (counterCreationDataCollection.Cast<CounterCreationData>().Any(x => x.CounterName == counterType))
                        {
                            Trace.TraceInformation("Counter type '{0}' was duplicate", counterType);
                            continue;
                        }

                        using (var counterHandler = PerfItRuntime.HandlerFactories[counterType](categoryName, instrumentationInfo.InstanceName))
                        {
                            counterCreationDataCollection.AddRange(counterHandler.BuildCreationData());
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

        public static void Uninstall(Assembly installerAssembly,
            IInstrumentationDiscoverer discoverer,
            string categoryName = null)
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

    }
}
#endif