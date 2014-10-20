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
    
    public interface IPerfItAttribute
    {
        /// <summary>
        /// Optional name of the counter. 
        /// If not specified it will be [ServiceClassName].[Method] for each counter.
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

      
    }
}
