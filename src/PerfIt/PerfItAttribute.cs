using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PerfIt
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PerfItAttribute :  Attribute, IPerfItAttribute
    {
        public PerfItAttribute()
        {
            Description = string.Empty;
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

      
    }
}
