using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfIt
{
    public interface IInstrumentor
    {
        void Instrument(Action aspect, double? samplingRate = null, InstrumentationContext extraContext = null);

        Task InstrumentAsync(Func<Task> asyncAspect, double? samplingRate = null, InstrumentationContext extraContext = null);

        IDictionary<string, ITwoStageTracer> Tracers { get; }
    }
}
