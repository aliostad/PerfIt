using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace PerfIt.Mvc
{
    public abstract class InstrumentationContextProviderBaseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
            if (!actionExecutedContext.HttpContext.Items.Contains(Constants.PerfItInstrumentationContextKey))
                actionExecutedContext.HttpContext.Items[Constants.PerfItInstrumentationContextKey] =
                    ProvideInstrumentationContext(actionExecutedContext);
        }

        protected abstract string ProvideInstrumentationContext(ActionExecutedContext actionExecutedContext);
    }
}
