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
    public class PerfItFilterAttribute : ActionFilterAttribute, IPerfItAttribute
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

       
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            bool raiseErrors = true;

            raiseErrors = PerfItRuntime.RaisePublishErrors;
                    
           

            try
            {
                var instanceName = InstanceName;
                if (string.IsNullOrEmpty(instanceName))
                {
                    
                    HttpActionDescriptor actionDescriptor = actionContext.ActionDescriptor;
                    instanceName = PerfItRuntime.GetCounterInstanceName(actionDescriptor.ControllerDescriptor.ControllerType,
                        actionDescriptor.ActionName);
                }

                actionContext.Request.Properties.Add(Constants.PerfItKey, new PerfItContext());

                if (actionContext.Request.Properties.ContainsKey(Constants.PerfItKey))
                {
                    var invocationContext = (PerfItContext)actionContext.Request.Properties[Constants.PerfItKey];

                    foreach (var counter in Counters)
                    {
                        invocationContext.CountersToRun.Add(PerfItRuntime.GetUniqueName(instanceName, counter));


                        PerfItRuntime.MonitoredCountersContexts[PerfItRuntime.GetUniqueName(instanceName, counter)].Handler.OnRequestStarting(invocationContext);
                       
                    }

                    invocationContext.Filter = this;
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError(exception.ToString());
                if(raiseErrors)
                    throw exception;
            }
            
        } 
    }
}
