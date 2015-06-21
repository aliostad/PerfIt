using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PerfIt.WebApi
{
    interface IInstanceNameProvider
    {
        string GetInstanceName(HttpActionContext actionContext);
    }
}
