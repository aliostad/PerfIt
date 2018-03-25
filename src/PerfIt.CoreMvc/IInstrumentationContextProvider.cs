using Microsoft.AspNetCore.Mvc.Filters;

namespace PerfIt.CoreMvc
{
    public interface IInstrumentationContextProvider
    {
        string GetContext(ActionExecutedContext actionExecutedContext);
    }
}
