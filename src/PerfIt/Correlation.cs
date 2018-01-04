using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if NET452
using System.Runtime.Remoting;
#endif

namespace PerfIt
{

//#if NET452

    public static class Correlation
    {
        public const string CorrelationIdKey = "corr-id";

        public static object GetId(string key = CorrelationIdKey, bool setIfNotThere = true)
        {
              
            var corrId = CallContext.LogicalGetData(key);
            if (corrId == null && setIfNotThere)
            {
                corrId = Guid.NewGuid();
                SetId(corrId, key);
            }

            return corrId;
        }

        public static void SetId(object id, string key = CorrelationIdKey)
        {
            CallContext.LogicalSetData(key, id);
        }
    }
//#endif
}
