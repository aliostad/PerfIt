using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#if NET452
using PerfIt.Handlers;
#else
using Microsoft.Extensions.Configuration;
#endif

namespace PerfIt
{
    public class InstrumentorCreatedArgs : EventArgs
    {
        public InstrumentorCreatedArgs(IInstrumentor instrumentor, IInstrumentationInfo info)
        {
            Info = info;
            Instrumentor = instrumentor;
        }

        public IInstrumentor Instrumentor { get; }

        public IInstrumentationInfo Info { get; }
    }

    public static class PerfItRuntime
    {
        /// <summary>
        /// Gets called when an instrumentor is created, e.g. SimpleInstrumentor.
        /// Useful for modifying instrumentors created by attributes.
        /// </summary>
        public static event EventHandler<InstrumentorCreatedArgs> InstrumentorCreated;

        static PerfItRuntime()
        {
#if NET452
            ConfigurationProvider = (s) => ConfigurationManager.AppSettings[s];
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

#else
            ConfigurationProvider = (s) => new ConfigurationBuilder()
                .AddInMemoryCollection().Build()[s];
#endif
           
        }

#if NET452
        /// <summary>
        /// Counter handler factories with counter type as the key.
        /// Factory's first param is applicationName and second is the filter
        /// Use it to register your own counters or replace built-in implementations
        /// </summary>
        public static Dictionary<string, Func<string, string, ICounterHandler>> HandlerFactories { get; private set; }
#endif

        internal static void OnInstrumentorCreated(InstrumentorCreatedArgs args)
        {
            if (InstrumentorCreated != null)
                InstrumentorCreated(args.Instrumentor, args);
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
            var categoryValue = GetConfigurationValue(featureName, ":", categoryName);
            if (!string.IsNullOrEmpty(categoryValue))
            {
                return bool.Parse(categoryValue);
            }

            categoryValue = GetConfigurationValue(featureName, "#", categoryName);
            if (!string.IsNullOrEmpty(categoryValue))
            {
                return bool.Parse(categoryValue);
            }

            var value = GetConfigurationValue(featureName, "#");
            if (!string.IsNullOrEmpty(value))
            {
                return bool.Parse(value);
            }

            value = GetConfigurationValue(featureName, ":");
            if (!string.IsNullOrEmpty(value))
            {
                return bool.Parse(value);
            }

            return defaultValue;
        }

        private static string GetConfigurationValue(string key, string delimiter, string categoryName = null)
        {
            if (categoryName == null)
                return ConfigurationProvider(string.Format("{0}{1}{2}", 
                    Constants.PerfItConfigurationPrefix, delimiter, key));
            else
                return ConfigurationProvider(string.Format("{0}{1}{2}{1}{3}", 
                    Constants.PerfItConfigurationPrefix, delimiter, key, categoryName));
        }

        public static double GetSamplingRate(string categoryName, double defaultValue)
        {
            var categoryValue = GetConfigurationValue(Constants.PerfItSamplingRate, ":", categoryName);
            if (!string.IsNullOrEmpty(categoryValue))
            {
                return double.Parse(categoryValue);
            }

            categoryValue = GetConfigurationValue(Constants.PerfItSamplingRate, "#", categoryName);
            if (!string.IsNullOrEmpty(categoryValue))
            {
                return double.Parse(categoryValue);
            }

            var value = GetConfigurationValue(Constants.PerfItSamplingRate, "#");
            if (!string.IsNullOrEmpty(value))
            {
                return double.Parse(value);
            }

            value = GetConfigurationValue(Constants.PerfItSamplingRate, ":");
            if (!string.IsNullOrEmpty(value))
            {
                return double.Parse(value);
            }

            return defaultValue;
        }


        /// <summary>
        /// By default uses appSettings. Set it to your own if you need to change it.
        /// </summary>
        public static Func<string, string> ConfigurationProvider { get; set; }


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
