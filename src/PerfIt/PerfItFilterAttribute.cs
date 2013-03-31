using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PerfIt
{
    public class PerfItFilterAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Optional name of the counter. If not specified it will be [controller].[action]
        /// </summary>
        public string Name { get; set; }

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

            if (actionExecutedContext.Request.Properties.ContainsKey(Constants.PerfItKey))
            {
                var context = (PerfItContext) actionExecutedContext.Request.Properties[Constants.PerfItKey];
                if (string.IsNullOrEmpty(Name))
                {
                    Name = string.Format("{0}.{1}",
                                         actionExecutedContext.ActionContext.ControllerContext.ControllerDescriptor
                                                              .ControllerName,
                                         actionExecutedContext.ActionContext.ActionDescriptor.ActionName);
                }

                context.Filter = this;
            }
        } 
    }
}
