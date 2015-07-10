using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfIt
{
    public interface IInstrumentationInfo
    {
        /// <summary>
        /// Optional name of the counter. 
        /// If not specified it will be [controller].[action] for each counter.
        /// If it is provided, make sure it is UNIQUE within the project
        /// </summary>
        string InstanceName { get; set; }

        /// <summary>
        /// Description of the counter. Will be published to counter metadata visible in Perfmon.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Counter types. Each value as a string.
        /// </summary>
        string[] Counters { get; set; }

        /// <summary>
        /// The categoryName of the stuff
        /// </summary>
        string CategoryName { get; set; }

    }
}
