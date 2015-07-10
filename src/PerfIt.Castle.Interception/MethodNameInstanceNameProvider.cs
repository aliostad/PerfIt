using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt.Castle.Interception
{
    public class MethodNameInstanceNameProvider : IInstanceNameProvider
    {
        string IInstanceNameProvider.GetInstanceName(System.Reflection.MethodInfo methodInfo)
        {
            return string.Format("{0}_{1}", methodInfo.DeclaringType.FullName, methodInfo.Name);
        }
    }
}
