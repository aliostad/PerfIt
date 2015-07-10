using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;



namespace PerfIt.Castle.Interception
{
    public interface IInstrumentationContextProvider
    {
        string GetContext(MethodInfo methodInfo);
    }
}
