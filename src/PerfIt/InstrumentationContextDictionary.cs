using System.Collections.Generic;

namespace PerfIt
{
    public class InstrumentationContextDictionary : Dictionary<string, object>
    {
        internal InstrumentationContextDictionary(IDictionary<string, object> dictionary = null)
            : base(dictionary ?? new Dictionary<string, object>())
        {
        }
    }
}
