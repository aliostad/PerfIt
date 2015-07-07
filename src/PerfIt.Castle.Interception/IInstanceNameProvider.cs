using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace PerfIt.Castle.Interception
{
    public interface IInstanceNameProvider
    {
        string GetInstanceName(MethodInfo methodInfo);
    }
}
