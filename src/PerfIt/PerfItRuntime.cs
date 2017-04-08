using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using PerfIt.Handlers;

namespace PerfIt
{
    public static class PerfItRuntime
    {
        static PerfItRuntime()
        {

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

            HandlerFactories.Add(CounterTypes.NumberOfErrorsPerSecond,
                (categoryName, instanceName) => new NumberOfErrorsPerSecondHandler(categoryName, instanceName));

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
        [Obsolete("Please use CounterInstaller.")]
        public static void Uninstall(Assembly installerAssembly, string categoryName = null)
        {
            CounterInstaller.Uninstall(installerAssembly, categoryName);    
        }


        /// <summary>
        /// Installs performance counters in the assembly
        /// </summary>
        /// <param name="installerAssembly"></param>
        /// <param name="discoverer">object that can discover aspects inside and assembly</param>
        /// <param name="categoryName">category name for the metrics. If not provided, it will use the assembly name</param>
        [Obsolete("Please use CounterInstaller.")]
        public static void Install(Assembly installerAssembly,
            IInstrumentationDiscoverer discoverer,
            string categoryName = null)
        {
            CounterInstaller.Install(installerAssembly, discoverer, categoryName);
        }

        [Obsolete("Please use CounterInstaller.")]
        public static void Uninstall(Assembly installerAssembly,
            IInstrumentationDiscoverer discoverer,
            string categoryName = null)
        {
            CounterInstaller.Uninstall(installerAssembly, discoverer, categoryName);
        }


        /// <summary>
        ///  installs 4 standard counters for the category provided
        /// </summary>
        /// <param name="categoryName"></param>
        [Obsolete("Please use CounterInstaller.")]
        public static void InstallStandardCounters(string categoryName)
        {
            CounterInstaller.InstallStandardCounters(categoryName);
        }

        /// <summary>
        ///  Uninstalls the category provided
        /// </summary>
        /// <param name="categoryName"></param>
        [Obsolete("Please use CounterInstaller.")]
        public static void Uninstall(string categoryName)
        {
            CounterInstaller.Uninstall(categoryName);
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
            var categoryValue = ConfigurationManager.AppSettings[string.Format("{0}:{1}", featureName, categoryName)];
            if (!string.IsNullOrEmpty(categoryValue))
            {
                return bool.Parse(categoryValue);
            }

            var value = ConfigurationManager.AppSettings[featureName];
            if (!string.IsNullOrEmpty(value))
            {
                return bool.Parse(value);
            }

            return defaultValue;
        }

        public static double GetSamplingRate(string categoryName, double defaultValue)
        {
            var categoryValue = ConfigurationManager.AppSettings[string.Format("{0}:{1}", Constants.PerfItSamplingRate, categoryName)];
            if (!string.IsNullOrEmpty(categoryValue))
            {
                return double.Parse(categoryValue);
            }

            var value = ConfigurationManager.AppSettings[Constants.PerfItSamplingRate];
            if (!string.IsNullOrEmpty(value))
            {
                return double.Parse(value);
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
