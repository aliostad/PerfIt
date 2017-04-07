using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
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
}
