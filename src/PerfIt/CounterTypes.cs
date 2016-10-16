using System;
using System.Collections.Generic;

namespace PerfIt
{
    // TODO: TBD: I wonder, should this be names or better off being Types?
    /// <summary>
    /// CounterTypes collections.
    /// </summary>
    public class CounterTypes
    {
        /// <summary>
        /// "AverageTimeTaken"
        /// </summary>
        public const string AverageTimeTaken = "AverageTimeTaken";

        /// <summary>
        /// "TotalNoOfOperations"
        /// </summary>
        public const string TotalNoOfOperations = "TotalNoOfOperations";

        /// <summary>
        /// "LastOperationExecutionTime"
        /// </summary>
        public const string LastOperationExecutionTime = "LastOperationExecutionTime";

        /// <summary>
        /// "NumberOfOperationsPerSecond"
        /// </summary>
        public const string NumberOfOperationsPerSecond = "NumberOfOperationsPerSecond";

        /// <summary>
        /// "NumberOfErrorsPerSecond"
        /// </summary>
        public const string NumberOfErrorsPerSecond = "NumberOfErrorsPerSecond";

        /// <summary>
        /// "CurrentConcurrentOperationsCount"
        /// </summary>
        public const string CurrentConcurrentOperationsCount = "CurrentConcurrentOperationsCount";

        /// <summary>
        /// Returns the StandardCounters.
        /// </summary>
        private static IEnumerable<string> GetStandardCounters()
        {
            yield return AverageTimeTaken;
            yield return TotalNoOfOperations;
            yield return LastOperationExecutionTime;
            yield return NumberOfOperationsPerSecond;
            yield return CurrentConcurrentOperationsCount;
        }

        /// <summary>
        /// LazyStandardCounters backing field.
        /// </summary>
        private static readonly Lazy<IEnumerable<string>> LazyStandardCounters
            = new Lazy<IEnumerable<string>>(GetStandardCounters);

        /// <summary>
        /// Gets the StandardCounters.
        /// </summary>
        public static IEnumerable<string> StandardCounters
        {
            get { return LazyStandardCounters.Value; }
        }
    }
}
