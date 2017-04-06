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

        public static string GetId(string key = CorrelationIdKey, bool setIfNotThere = true)
        {
            var corrId = CallContext.LogicalGetData(key) as string;
            if (corrId == null && setIfNotThere)
            {
                corrId = Guid.NewGuid().ToString("N");
                SetId(corrId, key);
            }

            return corrId;
        }

        public static void SetId(string id, string key = CorrelationIdKey)
        {
            CallContext.LogicalSetData(key, id);
        }
    }
}
