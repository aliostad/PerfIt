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
    public class PerfItFilterAttribute : ActionFilterAttribute
    {
        public PerfItFilterAttribute()
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

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);

            try
            {
                var instanceName = InstanceName;
                if (string.IsNullOrEmpty(instanceName))
                {
                    HttpActionContext actionContext = actionExecutedContext.ActionContext;
                    HttpActionDescriptor actionDescriptor = actionContext.ActionDescriptor;
                    instanceName = PerfItRuntime.GetCounterInstanceName(actionDescriptor.ControllerDescriptor.ControllerType,
                        actionDescriptor.ActionName);
                }

                if (actionExecutedContext.Request.Properties.ContainsKey(Constants.PerfItKey))
                {
                    var context = (PerfItContext)actionExecutedContext.Request.Properties[Constants.PerfItKey];

                    foreach (var counter in Counters)
                    {
                        context.CountersToRun.Add(PerfItRuntime.GetUniqueName(instanceName, counter));
                    }

                    context.Filter = this;
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
                if(PerfItRuntime.ThrowPublishingErrors)
                    throw exception;
            }
            
        } 
    }
}
