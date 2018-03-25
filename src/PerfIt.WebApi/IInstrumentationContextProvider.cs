using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace PerfIt.WebApi
{
    public interface IInstrumentationContextProvider
    {
        InstrumentationContext GetContext(HttpActionExecutedContext actionExecutedContext);
    }
}
