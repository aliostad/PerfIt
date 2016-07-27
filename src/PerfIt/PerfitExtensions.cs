using System.Collections.Generic;

namespace PerfIt
{
    public static class PerfItExtensions 
    {
       public static void SetContextToErrorState(this Dictionary<string, object> contexts)
        {
            contexts[Constants.PerfItContextHasErroredKey] = true;
        }
        
    }
}
