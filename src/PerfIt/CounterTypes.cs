using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfIt
{
    public class CounterTypes
    {
        public const string AverageTimeTaken = "AverageTimeTaken";
        public const string TotalNoOfOperations = "TotalNoOfOperations";
        public const string LastOperationExecutionTime = "LastOperationExecutionTime";
        public const string NumberOfOperationsPerSecond = "NumberOfOperationsPerSecond";
        public const string CurrentConcurrentOperationsCount = "CurrentConcurrentOperationsCount";

        public static readonly string[] StandardCounters = new[]
        {
            AverageTimeTaken,
            TotalNoOfOperations,
            LastOperationExecutionTime,
            NumberOfOperationsPerSecond,
            CurrentConcurrentOperationsCount
        };

    }
}
