using Microsoft.AspNetCore.Mvc.Filters;

namespace PerfIt.CoreMvc
{
    public interface IInstanceNameProvider
    {
        string GetInstanceName(ActionExecutingContext actionContext);
    }
}
