using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace PerfIt.Mvc
{
    public interface IInstanceNameProvider
    {
        string GetInstanceName(ActionExecutingContext actionContext);
    }
}
