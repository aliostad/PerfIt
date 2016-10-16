using System;
using System.Web.Mvc;

namespace PerfIt.Mvc
{
    [AttributeUsage(AttributeTargets.Method)]
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
