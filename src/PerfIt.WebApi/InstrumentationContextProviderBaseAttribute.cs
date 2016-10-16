using System;
using System.Web.Http.Filters;

namespace PerfIt.WebApi
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class InstrumentationContextProviderBaseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
            if (!actionExecutedContext.Request.Properties.ContainsKey(Constants.PerfItInstrumentationContextKey))
                actionExecutedContext.Request.Properties.Add(Constants.PerfItInstrumentationContextKey,
                    ProvideInstrumentationContext(actionExecutedContext));
        }

        protected abstract string ProvideInstrumentationContext(HttpActionExecutedContext actionExecutedContext);
    }
}
