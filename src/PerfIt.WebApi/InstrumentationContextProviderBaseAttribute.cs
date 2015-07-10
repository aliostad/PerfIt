using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace PerfIt.WebApi
{
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
