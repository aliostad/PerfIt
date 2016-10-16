using System.Diagnostics;

namespace PerfIt.Castle.Interception
{
    // TODO: TBD: could probably have the Attribute, IInstrumentationInfoHost pattern repeated throughout and capture the Discoverers as a common cross cutting concern... But for minor Mvc, WebApi, etc, differences.
    public class PerfItAttribute : InstrumentationInfoAttributeBase
    {
        public PerfItAttribute()
            : this(string.Empty)
        {
            Trace.TraceWarning(
                "Performance Counter not specified at the Method level. Make sure you set it's at least set at the class level");
        }

        public PerfItAttribute(string categoryName, string description = null)
            : base(categoryName, description)
        {
        }
    }
}
