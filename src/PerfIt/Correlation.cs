using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if NET452
using System.Runtime.Remoting.Messaging;
#else

public static class CallContext
{
    static ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();

    /// <summary>
    /// Stores a given object and associates it with the specified name.
    /// </summary>
    /// <param name="name">The name with which to associate the new item in the call context.</param>
    /// <param name="data">The object to store in the call context.</param>
    public static void LogicalSetData(string name, object data) =>
        state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;

    /// <summary>
    /// Retrieves an object with the specified name from the <see cref="CallContext"/>.
    /// </summary>
    /// <param name="name">The name of the item in the call context.</param>
    /// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
    public static object LogicalGetData(string name) =>
        state.TryGetValue(name, out AsyncLocal<object> data) ? data.Value : null;
}

#endif

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
