using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;


namespace PerfIt.Castle.Interception
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PerfItAttribute : Attribute, IInstrumentationInfo
    {

        public PerfItAttribute():this("")
        {
            Trace.TraceWarning("Performance Counter not specified at the Method level. Make sure you set it's at least set at the class level");

       }

        public PerfItAttribute(string categoryName)
        {
            Description = string.Empty;
            

            CategoryName = categoryName;
        }

        /// <summary>
        /// Optional name of the counter. 
        /// If not specified it will be [controller].[action] for each counter.
        /// If it is provided, make sure it is UNIQUE within the project
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// Description of the counter. Will be published to counter metadata visible in Perfmon.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Counter types. Each value as a string.
        /// </summary>
        public string[] Counters { get; set; }

        public string CategoryName { get; set; }

      
    }
}
