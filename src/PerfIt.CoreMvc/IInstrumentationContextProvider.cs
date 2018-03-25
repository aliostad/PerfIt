using Microsoft.AspNetCore.Mvc.Filters;

namespace PerfIt.CoreMvc
{
    public interface IInstrumentationContextProvider
    {
        InstrumentationContext GetContext(ActionExecutedContext actionExecutedContext);
    }
}
