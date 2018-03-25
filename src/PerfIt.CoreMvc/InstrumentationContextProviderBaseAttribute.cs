using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt.CoreMvc
{
    public abstract class InstrumentationContextProviderBaseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
            if (!actionExecutedContext.HttpContext.Items.ContainsKey(Constants.PerfItInstrumentationContextKey))
                actionExecutedContext.HttpContext.Items[Constants.PerfItInstrumentationContextKey] =
                    ProvideInstrumentationContext(actionExecutedContext);
        }

        protected abstract string ProvideInstrumentationContext(ActionExecutedContext actionExecutedContext);
    }
}
