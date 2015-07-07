using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;



namespace PerfIt.Castle.Interception
{
    public class InstrumentationContextProvider : IInstrumentationContextProvider
    {
        public string GetContext(MethodInfo methodInfo)
        {

            return string.Format("{0}_{1}", methodInfo.ReflectedType.FullName, methodInfo.Name);

        }

    }
}
